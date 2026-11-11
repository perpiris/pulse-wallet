using PulseWallet.Api.Extensions;
using PulseWallet.Application;
using PulseWallet.Infrastructure;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSerilogLogging(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi()
    .AddDatabaseHealthChecks(builder.Configuration);

WebApplication app = builder.Build();

// The middleware pipeline stays here, in order: the order matters, so it should be visible at a glance.
app.UseSerilogRequestLogging(); // outermost, so it logs the final status code (after errors are turned into 4xx/5xx)
app.UseExceptionHandler();      // next, so it catches exceptions from everything after it
app.UseApiDocumentation();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();