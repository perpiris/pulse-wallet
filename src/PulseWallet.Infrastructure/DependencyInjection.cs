using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PulseWallet.Application.Abstractions;
using PulseWallet.Application.ExchangeRates;
using PulseWallet.Application.Transfers;
using PulseWallet.Application.Wallets;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Transfers;
using PulseWallet.Domain.Wallets;
using PulseWallet.Infrastructure.ExchangeRates;
using PulseWallet.Infrastructure.Http;
using PulseWallet.Infrastructure.Persistence;
using PulseWallet.Infrastructure.Persistence.Queries;
using PulseWallet.Infrastructure.Persistence.Repositories;

namespace PulseWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Comes from the ConnectionStrings__PulseWallet environment variable in Docker (compose.yaml),
        // or from user secrets when running locally. Never from appsettings, so no password is committed.
        string connectionString = configuration.GetConnectionString("PulseWallet")
            ?? throw new InvalidOperationException("Connection string 'PulseWallet' is not configured.");

        // Scoped by default: one DbContext per HTTP request.
        services.AddDbContext<PulseWalletDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ILedgerEntryRepository, LedgerEntryRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IWalletQueries, WalletQueries>();
        services.AddScoped<ITransferQueries, TransferQueries>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PulseWalletDbContext>());

        services.AddExchangeRates(configuration);

        return services;
    }

    private static void AddExchangeRates(this IServiceCollection services, IConfiguration configuration)
    {
        // Must end with '/': relative paths like "latest" replace the last segment of a base address without one,
        // so "https://api.frankfurter.dev/v1" + "latest" would become "https://api.frankfurter.dev/latest".
        string? baseUrl = configuration["ExchangeRates:Frankfurter:BaseUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseAddress) || !baseAddress.AbsoluteUri.EndsWith('/'))
        {
            throw new InvalidOperationException("ExchangeRates:Frankfurter:BaseUrl must be an absolute URL ending with '/'.");
        }

        // One breaker per external service, shared by every request to it. Keyed, so a second HTTP client
        // added later gets its own breaker instead of silently sharing this one.
        const string clientKey = nameof(FrankfurterExchangeRateProvider);
        services.AddKeyedSingleton<CircuitBreaker>(clientKey, (sp, _) =>
            new CircuitBreaker(failureThreshold: 5, breakDuration: TimeSpan.FromSeconds(30), sp.GetRequiredService<TimeProvider>()));

        // Typed client: registered as transient, while the factory pools and recycles the handler underneath.
        // Never inject it into a singleton, or that singleton keeps one handler forever and misses DNS changes.
        //
        // A user waits on a transfer while this runs, so everything is tuned to fail fast:
        // 2s per try, 2 retries with backoff, 8s in total (HttpClient.Timeout wraps all the handlers).
        // Handlers run in the order they're added, each wrapping the next: retry → breaker → attempt timeout → network.
        // The breaker sits inside the retry so it counts every single try, and a timed-out try counts as a failure.
        // Handlers only read the cache. The cached provider has no state of its own (IMemoryCache is a singleton),
        // so it can be a singleton too. Frankfurter is registered as its concrete type, used only by the loader.
        services.AddMemoryCache();
        services.AddSingleton<IExchangeRateProvider, CachedExchangeRateProvider>();
        services.AddScoped<ExchangeRateCacheLoader>();

        // An Application type, registered here because this is where the exchange-rate settings are read.
        int maxRateAgeInDays = configuration.GetValue<int?>("ExchangeRates:MaxRateAgeInDays")
            ?? throw new InvalidOperationException("ExchangeRates:MaxRateAgeInDays is not configured.");
        services.AddSingleton(sp => new ExchangeRateAgePolicy(sp.GetRequiredService<TimeProvider>(), maxRateAgeInDays));

        TimeSpan refreshInterval = configuration.GetValue<TimeSpan?>("ExchangeRates:RefreshInterval")
            ?? throw new InvalidOperationException("ExchangeRates:RefreshInterval is not configured.");
        services.AddHostedService(sp => new ExchangeRateRefreshService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<TimeProvider>(),
            refreshInterval,
            sp.GetRequiredService<ILogger<ExchangeRateRefreshService>>()));

        services.AddHttpClient<FrankfurterExchangeRateProvider>(client =>
            {
                client.BaseAddress = baseAddress;
                client.Timeout = TimeSpan.FromSeconds(8);
            })
            .AddHttpMessageHandler(sp =>
                new RetryHandler(maxRetries: 2, baseDelay: TimeSpan.FromMilliseconds(300), sp.GetRequiredService<TimeProvider>()))
            .AddHttpMessageHandler(sp => new CircuitBreakerHandler(sp.GetRequiredKeyedService<CircuitBreaker>(clientKey)))
            .AddHttpMessageHandler(() => new AttemptTimeoutHandler(TimeSpan.FromSeconds(2)));
    }
}