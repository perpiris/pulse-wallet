using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.ExchangeRates;

// Decides when a rate is too old to use without the sender's explicit agreement. Stale rates only reach the cache
// when Frankfurter has been down for a while (the database fallback, or a refresh that keeps failing).
// Counted in calendar days from the publish date: the ECB publishes once per working day, so a rate from Friday
// is still the latest one on Monday, and the limit has to allow for weekends and holidays.
public class ExchangeRateAgePolicy
{
    private readonly TimeProvider _timeProvider;

    public ExchangeRateAgePolicy(TimeProvider timeProvider, int maxAgeInDays)
    {
        if (maxAgeInDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAgeInDays), maxAgeInDays, "The maximum rate age can't be negative.");
        }

        _timeProvider = timeProvider;
        MaxAgeInDays = maxAgeInDays;
    }

    public int MaxAgeInDays { get; }

    public bool IsStale(ExchangeRate rate)
    {
        DateOnly today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        return today.DayNumber - rate.Date.DayNumber > MaxAgeInDays;
    }
}