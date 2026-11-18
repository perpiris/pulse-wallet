using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using PulseWallet.Api.ErrorHandling;
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
        });

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

    public static IServiceCollection AddIdentityAuthentication(this IServiceCollection services)
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
        services.AddIdentityApiEndpoints<ApplicationUser>(options =>
            {
                // Length beats composition rules (NIST SP 800-63B); the default composition rules are kept too.
                options.Password.RequiredLength = 12;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<PulseWalletDbContext>();

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