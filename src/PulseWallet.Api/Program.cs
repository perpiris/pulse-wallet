using PulseWallet.Api.Extensions;
using PulseWallet.Application;
using PulseWallet.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSerilogLogging(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi()
    .AddDatabaseHealthChecks(builder.Configuration);

var app = builder.Build();

// The middleware pipeline stays here, in order: the order matters, so it should be visible at a glance.
app.UseApiDocumentation();
app.UseSerilogRequestLogging();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();