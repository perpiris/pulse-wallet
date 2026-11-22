using PulseWallet.Domain.Shared;

namespace PulseWallet.Infrastructure.ExchangeRates;

// The last rate fetched for each pair, kept as a fallback for when Frankfurter is down (above all at startup,
// when the cache is empty). A persistence model, not a domain entity: ExchangeRate is a value object with no
// identity, and this row only exists so the cache can be refilled from it. One row per pair, overwritten on
// every successful fetch; there's no history.
public class SavedExchangeRate
{
    public Currency From { get; set; }

    public Currency To { get; set; }

    public decimal Rate { get; set; }

    // The day the source published the rate, as on ExchangeRate.
    public DateOnly Date { get; set; }

    // When we last fetched it: says whether the refresh is still working, separate from how old the rate is.
    public DateTimeOffset FetchedAt { get; set; }
}