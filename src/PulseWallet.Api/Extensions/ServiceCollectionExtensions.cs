using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PulseWallet.Api.ErrorHandling;
using PulseWallet.Application.Telemetry;
using PulseWallet.Infrastructure.Identity;
using PulseWallet.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

namespace PulseWallet.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSerilogLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Levels, overrides and properties come from appsettings; the console format is chosen here.
        // (Configuration merges WriteTo arrays by index, so overriding the sink per environment in
        // appsettings would hand Serilog both a formatter and a theme for the same sink.)
        services.AddSerilog((serviceProvider, config) =>
        {
            config
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(serviceProvider)
                .Enrich.FromLogContext();

            if (environment.IsDevelopment())
            {
                // Readable, coloured text for a person watching the terminal. Text output shows no properties by
                // default, so {ProblemDetail} is added to say why a request failed. It is empty on every other line.
                config.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {ProblemDetail:l}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture,
                    theme: AnsiConsoleTheme.Code);
            }
            else
            {
                // One JSON object per line: every property (TraceId, RequestId, WalletId...) becomes a searchable
                // field in a log collector, and there are no ANSI colour codes in container logs.
                config.WriteTo.Console(new RenderedCompactJsonFormatter());
            }
        },
        // Serilog replaces the logging pipeline, so other logger providers get nothing by default. With this, every
        // event Serilog accepts is also handed to them, including OpenTelemetry's (added by UseOtlpExporter): logs reach
        // the OTLP backend with their TraceId and SpanId, next to the trace they belong to. No extra sink package needed.
        writeToProviders: true);

        return services;
    }

    public static IServiceCollection AddOpenTelemetryTracing(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            // Every span and metric carries service.name, so one backend can hold several services apart.
            .ConfigureResource(resource => resource.AddService(serviceName: "PulseWallet.Api"))
            .WithTracing(tracing => tracing
                // The root span of every request. Health checks are probed every few seconds by Docker and
                // load balancers: thousands of identical traces that say nothing, so they're dropped here.
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                // Since .NET 9 HttpClient creates its own spans (source "System.Net.Http"), so no package is needed.
                // The span is created next to the socket, inside our retry handler: every retry is its own span.
                .AddSource("System.Net.Http")
                // SqlClient doesn't create spans itself yet; this package turns its diagnostic events into spans.
                // EF Core goes through SqlClient, so every query and SaveChanges shows up.
                .AddSqlClientInstrumentation()
                .AddSource(PulseWalletTelemetry.SourceName))
            .WithMetrics(metrics => metrics
                // ASP.NET Core, HttpClient and the runtime publish their own meters since .NET 8/9; listening is enough.
                .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel", "System.Net.Http", "System.Runtime"))
            // Sends the rendered text ("Wallet 01a1... created") along with the template and its properties;
            // without it, the backend shows only the template ("Wallet {WalletId} created").
            .WithLogging(configureBuilder: null, configureOptions: options => options.IncludeFormattedMessage = true);

        // OTEL_EXPORTER_OTLP_ENDPOINT is the standard variable every OpenTelemetry SDK reads (set in compose.yaml and
        // launchSettings.json). Without it nothing is exported, instead of retrying a collector that isn't there.
        // UseOtlpExporter sends traces, metrics and logs; OTLP is the vendor-neutral protocol that the Aspire
        // Dashboard, Elastic and Azure Monitor all accept, so switching backends is configuration, not code.
        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            services.AddOpenTelemetry().UseOtlpExporter();
        }

        return services;
    }

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        // Enums travel as text ("EUR"), never as numbers. ASP.NET Core keeps two separate JSON settings,
        // so the converter is registered in both:
        // 1. Controllers: how request bodies are read and responses are written at runtime.
        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

        // 2. Minimal APIs and the OpenAPI generator: makes the OpenAPI document (and Swagger) show "EUR" instead of 0.
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

        services.AddOpenApi();

        // Errors: every exception becomes a ProblemDetails response; GlobalExceptionHandler picks the status code.
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static IServiceCollection AddIdentityAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Secure by default: an endpoint with no [Authorize] / [AllowAnonymous] of its own requires a signed-in
        // user, so a new endpoint that forgets [Authorize] fails closed (401) instead of being public.
        // The few public endpoints opt out explicitly with AllowAnonymous (health, OpenAPI, sign-in).
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        // Registers ASP.NET Core Identity (users, password hashing, lockout) plus the bearer token
        // authentication scheme that the /api/auth/login endpoint issues tokens for.
        // The tokens are ASP.NET Core's own format, protected by Data Protection: opaque to clients, not JWTs.
        services.AddIdentityApiEndpoints<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<PulseWalletDbContext>();

        // The password policy comes from configuration ("Identity:Password" in appsettings), so it can differ per
        // environment without code: strict in appsettings.json, length-only in appsettings.Development.json.
        // Bound onto IdentityOptions.Password: that's the object Identity reads; a separate
        // Configure<PasswordOptions> would be registered but never used.
        services.Configure<IdentityOptions>(options => configuration.GetSection("Identity:Password").Bind(options.Password));

        return services;
    }

    public static IServiceCollection AddDatabaseHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // Comes from the ConnectionStrings__PulseWallet environment variable in Docker (compose.yaml),
        // or from user secrets when running locally. Never from appsettings, so no password is committed.
        string connectionString = configuration.GetConnectionString("PulseWallet")
            ?? throw new InvalidOperationException("Connection string 'PulseWallet' is not configured.");

        services.AddHealthChecks()
            .AddSqlServer(connectionString, name: "sqlserver"); // Unhealthy if the database can't be reached

        return services;
    }
}