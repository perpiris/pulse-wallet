using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PulseWallet.Api.ErrorHandling;
using PulseWallet.Api.RateLimiting;
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
        // WebApplication.CreateBuilder registers the default providers (Console, Debug, EventSource, EventLog).
        // writeToProviders below hands every event to all registered providers, so the default Console provider
        // would print each line a second time next to Serilog's own console sink. Removed here, before
        // AddOpenTelemetryTracing adds OpenTelemetry's provider, which is the only one that should receive them.
        services.AddLogging(logging => logging.ClearProviders());

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

    public static IServiceCollection AddReverseProxySupport(this IServiceCollection services, IConfiguration configuration)
    {
        // Behind nginx every request comes from nginx's address; the real client is in X-Forwarded-For.
        // That header is plain text anyone can send, so it's only believed from known proxies: the nginx container
        // (its fixed address comes from compose.yaml) and loopback, which the framework trusts by default.
        // A request straight to the API's published port comes from Docker's gateway, so its header is ignored.
        string[] knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // ForwardLimit stays 1: only the last entry is used, the one nginx appended. Anything a client put in
            // front of it ("X-Forwarded-For: 1.2.3.4" to get a fresh rate limit bucket) is ignored.
            foreach (string proxy in knownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });

        return services;
    }

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        RateLimitingOptions limits = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? throw new InvalidOperationException("Rate limiting is not configured.");

        services.AddRateLimiter(options =>
        {
            // Every request, on every endpoint, counts towards its client's bucket. Each partition key gets its own
            // limiter, created on first use; QueueLimit stays 0, so an over-limit request is rejected at once
            // instead of being held open on the server.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(GetClientPartitionKey(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.Global.PermitLimit,
                    Window = limits.Global.Window
                }));

            // Token bucket, not fixed window: a fixed window lets a client spend its whole limit at the end of one
            // window and again at the start of the next, twice the limit within seconds. A bucket allows a small
            // burst, then only the refill rate. (A sliding window also avoids the edge, but its rejections carry
            // no RetryAfter, so there'd be no Retry-After header.)
            options.AddPolicy(RateLimitPolicies.MoneyMovement, context =>
                RateLimitPartition.GetTokenBucketLimiter(GetClientPartitionKey(context), _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = limits.MoneyMovement.TokenLimit,
                    TokensPerPeriod = limits.MoneyMovement.TokensPerPeriod,
                    ReplenishmentPeriod = limits.MoneyMovement.ReplenishmentPeriod
                }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                HttpContext httpContext = context.HttpContext;

                // Tells a well-behaved client how long to wait instead of retrying in a tight loop.
                // Whole seconds, rounded up: the header's delay-seconds form allows no fractions.
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    int seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                    httpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                }

                const string title = "Too many requests";
                const string detail = "Too many requests. Wait before trying again.";

                // Same as GlobalExceptionHandler: the request log line says why the request got a 429.
                IDiagnosticContext diagnosticContext = httpContext.RequestServices.GetRequiredService<IDiagnosticContext>();
                diagnosticContext.Set("ProblemTitle", title);
                diagnosticContext.Set("ProblemDetail", detail);

                IProblemDetailsService problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails =
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = title,
                        Detail = detail
                    }
                });
            };
        });

        return services;
    }

    // Signed-in requests are counted per user, so one user can't get more by switching networks, and users behind
    // the same office NAT don't share a bucket. Anonymous requests (register, login, refresh) are counted per IP.
    // Runs after UseAuthentication, so HttpContext.User is already filled from the bearer token.
    private static string GetClientPartitionKey(HttpContext context)
    {
        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            return $"user:{userId}";
        }

        return $"ip:{context.Connection.RemoteIpAddress}";
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