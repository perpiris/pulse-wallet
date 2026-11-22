using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PulseWallet.Infrastructure.ExchangeRates;

// Reloads the cache on a fixed interval, inside the API process. Not a cron job: the cache is this process's memory,
// so only code running in the process can refill it. With several instances, each one refreshes its own cache.
// The first load happens at startup (LoadExchangeRatesAsync in Program.cs), so this waits one interval before its first run.
public sealed partial class ExchangeRateRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _interval;
    private readonly ILogger<ExchangeRateRefreshService> _logger;

    public ExchangeRateRefreshService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        TimeSpan interval,
        ILogger<ExchangeRateRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _interval = interval;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // PeriodicTimer doesn't overlap runs: if a refresh takes longer than the interval, the missed tick fires
        // once when it finishes, never twice in parallel. The TimeProvider overload lets a test drive the clock.
        using PeriodicTimer timer = new PeriodicTimer(_interval, _timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // A hosted service is a singleton, while the loader holds a typed HttpClient (transient):
                // a scope per run gets a fresh one each time, so DNS changes are picked up.
                await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                ExchangeRateCacheLoader loader = scope.ServiceProvider.GetRequiredService<ExchangeRateCacheLoader>();
                await loader.LoadAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // An unhandled exception in a BackgroundService stops the whole host (the default since .NET 8).
                // A failed refresh only means the cache keeps the previous rates until the next tick.
                LogRefreshFailed(exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Exchange rate refresh failed; keeping the cached rates until the next run")]
    private partial void LogRefreshFailed(Exception exception);
}