using Microsoft.Extensions.Caching.Memory;
using PulseWallet.Application.ExchangeRates;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Infrastructure.ExchangeRates;

// What Application gets for IExchangeRateProvider: reads only from the cache, never calls Frankfurter itself.
// A transfer no longer waits on an external API (up to 8s with retries), and a burst of transfers doesn't turn
// into a burst of calls to the provider. The cache is filled at startup by ExchangeRateCacheLoader.
public class CachedExchangeRateProvider : IExchangeRateProvider
{
    private readonly IMemoryCache _cache;

    public CachedExchangeRateProvider(IMemoryCache cache)
    {
        _cache = cache;
    }

    // A miss means loading failed for this pair (the provider was down at startup): 503, as before, and
    // same-currency transfers keep working. Nothing refills the cache yet; a background refresh comes later.
    public Task<ExchangeRate> GetLatestRateAsync(Currency from, Currency to, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(ExchangeRateCacheKeys.For(from, to), out ExchangeRate? rate) && rate is not null)
        {
            return Task.FromResult(rate);
        }

        return Task.FromException<ExchangeRate>(new ExchangeRateUnavailableException(from, to));
    }
}