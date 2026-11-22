using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PulseWallet.Application.ExchangeRates;
using PulseWallet.Domain.Shared;
using PulseWallet.Infrastructure.Persistence;

namespace PulseWallet.Infrastructure.ExchangeRates;

// Fills the cache with every supported pair: 3 currencies → 3 calls (one per base) → 6 rates.
// Every successful fetch is also saved to the database; when a fetch fails, the saved rates fill the cache instead.
public partial class ExchangeRateCacheLoader
{
    // NeverRemove: IMemoryCache may evict entries under memory pressure, and there's no read-through on a miss.
    // Six tiny entries cost nothing to keep. No expiration either: rates stay until the next load replaces them.
    private static readonly MemoryCacheEntryOptions EntryOptions = new MemoryCacheEntryOptions
    {
        Priority = CacheItemPriority.NeverRemove
    };

    private readonly FrankfurterExchangeRateProvider _source;
    private readonly PulseWalletDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExchangeRateCacheLoader> _logger;

    public ExchangeRateCacheLoader(
        FrankfurterExchangeRateProvider source,
        PulseWalletDbContext dbContext,
        IMemoryCache cache,
        TimeProvider timeProvider,
        ILogger<ExchangeRateCacheLoader> logger)
    {
        _source = source;
        _dbContext = dbContext;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // Never throws when the provider or the database is down: the API starts anyway, cross-currency transfers get
    // 503 and same-currency ones keep working. Failing startup instead would put the whole API in a restart loop
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
                // The cause is already logged by the provider.
                await LoadSavedAsync(from, cancellationToken);
                continue;
            }

            foreach (ExchangeRate rate in rates)
            {
                _cache.Set(ExchangeRateCacheKeys.For(rate.From, rate.To), rate, EntryOptions);
            }

            LogBaseLoaded(from, rates.Count, rates[0].Date);

            await SaveAsync(from, rates, cancellationToken);
        }
    }

    // Upserts one row per pair. A failed save is only logged: the fresh rates are already in the cache,
    // and the next successful refresh saves again.
    private async Task SaveAsync(Currency from, IReadOnlyList<ExchangeRate> rates, CancellationToken cancellationToken)
    {
        try
        {
            Dictionary<Currency, SavedExchangeRate> saved = await _dbContext.ExchangeRates
                .Where(r => r.From == from)
                .ToDictionaryAsync(r => r.To, cancellationToken);

            DateTimeOffset now = _timeProvider.GetUtcNow();
            foreach (ExchangeRate rate in rates)
            {
                if (!saved.TryGetValue(rate.To, out SavedExchangeRate? row))
                {
                    row = new SavedExchangeRate { From = rate.From, To = rate.To };
                    _dbContext.ExchangeRates.Add(row);
                }

                row.Rate = rate.Rate;
                row.Date = rate.Date;
                row.FetchedAt = now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        // DbUpdateException also covers two instances inserting the same pair at the same moment (duplicate key):
        // the other one saved the same rates, so nothing is lost.
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The context is shared by all three bases in this run: drop the failed changes so they aren't
            // retried by the next base's SaveChanges.
            _dbContext.ChangeTracker.Clear();
            LogSaveFailed(exception, from);
        }
    }

    private async Task LoadSavedAsync(Currency from, CancellationToken cancellationToken)
    {
        List<SavedExchangeRate> saved;
        try
        {
            saved = await _dbContext.ExchangeRates
                .AsNoTracking()
                .Where(r => r.From == from)
                .ToListAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogSavedRatesUnreadable(exception, from);
            LogBaseNotLoaded(from);
            return;
        }

        if (saved.Count == 0)
        {
            LogBaseNotLoaded(from);
            return;
        }

        foreach (SavedExchangeRate row in saved)
        {
            ExchangeRate rate = ExchangeRate.Create(row.From, row.To, row.Rate, row.Date);
            string key = ExchangeRateCacheKeys.For(rate.From, rate.To);

            // On a refresh the cache may hold something newer than the database, if an earlier save failed.
            // A fallback must never replace a newer rate with an older one.
            if (_cache.TryGetValue(key, out ExchangeRate? cached) && cached is not null && cached.Date >= rate.Date)
            {
                continue;
            }

            _cache.Set(key, rate, EntryOptions);
        }

        LogBaseLoadedFromDatabase(from, saved.Count, saved.Min(r => r.Date));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Cached {Count} exchange rates from {From} (published {RateDate})")]
    private partial void LogBaseLoaded(Currency from, int count, DateOnly rateDate);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Frankfurter unavailable; cached {Count} saved exchange rates from {From} (published {RateDate})")]
    private partial void LogBaseLoadedFromDatabase(Currency from, int count, DateOnly rateDate);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exchange rates from {From} could not be saved to the database")]
    private partial void LogSaveFailed(Exception exception, Currency from);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Saved exchange rates from {From} could not be read from the database")]
    private partial void LogSavedRatesUnreadable(Exception exception, Currency from);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Exchange rates from {From} could not be loaded; transfers from {From} to another currency will fail until they are")]
    private partial void LogBaseNotLoaded(Currency from);
}