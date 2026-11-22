using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PulseWallet.Application.ExchangeRates;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Infrastructure.ExchangeRates;

// Fills the cache with every supported pair: 3 currencies → 3 calls (one per base) → 6 rates.
public partial class ExchangeRateCacheLoader
{
    // NeverRemove: IMemoryCache may evict entries under memory pressure, and there's no read-through on a miss.
    // Six tiny entries cost nothing to keep. No expiration either: rates stay until the next load replaces them.
    private static readonly MemoryCacheEntryOptions EntryOptions = new MemoryCacheEntryOptions
    {
        Priority = CacheItemPriority.NeverRemove
    };

    private readonly FrankfurterExchangeRateProvider _source;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExchangeRateCacheLoader> _logger;

    public ExchangeRateCacheLoader(
        FrankfurterExchangeRateProvider source,
        IMemoryCache cache,
        ILogger<ExchangeRateCacheLoader> logger)
    {
        _source = source;
        _cache = cache;
        _logger = logger;
    }

    // Never throws when the provider is down: the API starts anyway, cross-currency transfers get 503 and
    // same-currency ones keep working. Failing startup instead would put the whole API in a restart loop
    // because of one external dependency. Each base is loaded on its own, so one failure doesn't drop the rest.
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        foreach (Currency from in Enum.GetValues<Currency>())
        {
            IReadOnlyList<ExchangeRate> rates;
            try
            {
                rates = await _source.GetLatestRatesAsync(from, cancellationToken);
            }
            catch (ExchangeRateUnavailableException)
            {
                // The cause is already logged by the provider; this line says what it means for the app.
                LogBaseNotLoaded(from);
                continue;
            }

            foreach (ExchangeRate rate in rates)
            {
                _cache.Set(ExchangeRateCacheKeys.For(rate.From, rate.To), rate, EntryOptions);
            }

            LogBaseLoaded(from, rates.Count, rates[0].Date);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Cached {Count} exchange rates from {From} (published {RateDate})")]
    private partial void LogBaseLoaded(Currency from, int count, DateOnly rateDate);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Exchange rates from {From} could not be loaded; transfers from {From} to another currency will fail until they are")]
    private partial void LogBaseNotLoaded(Currency from);
}