using System.Globalization;
using PulseWallet.Api.Extensions;
using PulseWallet.Application;
using PulseWallet.Infrastructure;
using Serilog;

// Logs startup failures (missing connection string, bad configuration) before the real logger exists.
// AddSerilog replaces it once the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Services
        .AddSerilogLogging(builder.Configuration, builder.Environment)
        .AddOpenTelemetryTracing(builder.Configuration)
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApi()
        .AddIdentityAuthentication(builder.Configuration)
        .AddReverseProxySupport(builder.Configuration)
        .AddApiRateLimiting(builder.Configuration)
        .AddDatabaseHealthChecks(builder.Configuration);

    WebApplication app = builder.Build();

    await app.ApplyMigrationsInDevelopmentAsync();
    await app.LoadExchangeRatesAsync();

    // The middleware pipeline stays here, in order: the order matters, so it should be visible at a glance.
    app.UseForwardedHeaders();      // first, so everything after it (request log, rate limiter) sees the real client IP
    app.UseSerilogRequestLogging(); // outermost after that, so it logs the final status code (after errors are turned into 4xx/5xx)
    app.UseExceptionHandler();      // next, so it catches exceptions from everything after it
    app.UseApiDocumentation();
    app.UseAuthentication();       // reads the bearer token into HttpContext.User
    app.UseRateLimiter();          // after authentication, so it can count per user; before authorization, so floods of 401s count too
    app.UseAuthorization();        // then checks [Authorize] requirements against that user
    app.MapControllers();
    app.MapAuthEndpoints();
    app.MapHealthChecks("/health")
        .AllowAnonymous()          // load balancers and Docker probe it without signing in
        .DisableRateLimiting();    // and every few seconds: a throttled probe would mark a healthy instance as down

    app.Run();

    return 0;
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "PulseWallet terminated unexpectedly");

    // A non-zero exit code tells Docker / the orchestrator the process crashed, so it restarts it.
    return 1;
}
finally
{
    Log.CloseAndFlush();
}