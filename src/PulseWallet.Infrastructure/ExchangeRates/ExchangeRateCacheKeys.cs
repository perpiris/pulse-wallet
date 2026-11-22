using PulseWallet.Domain.Shared;

namespace PulseWallet.Infrastructure.ExchangeRates;

// The one place the key format lives, so the loader that writes and the provider that reads can't drift apart.
// Prefixed because IMemoryCache is one shared store for the whole process.
internal static class ExchangeRateCacheKeys
{
    public static string For(Currency from, Currency to)
    {
        return $"exchange-rate:{from}:{to}";
    }
}