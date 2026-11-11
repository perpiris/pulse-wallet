using System.Text.Json.Serialization;
using PulseWallet.Api.ErrorHandling;
using Serilog;

namespace PulseWallet.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Serilog, configured from the "Serilog" section of appsettings (levels, sinks, properties).
    /// </summary>
    public static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((serviceProvider, config) => config
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(serviceProvider)
            .Enrich.FromLogContext());

        return services;
    }

    /// <summary>
    /// Controllers, JSON settings and the OpenAPI document.
    /// </summary>
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

    /// <summary>
    /// Health checks for /health, including whether SQL Server can be reached.
    /// </summary>
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