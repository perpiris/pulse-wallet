using PulseWallet.Application.ExchangeRates;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.UnitTests.ExchangeRates;

public sealed class ExchangeRateAgePolicyTests
{
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ExchangeRateAgePolicy _policy;

    public ExchangeRateAgePolicyTests()
    {
        // Late in the day on purpose: the age counts calendar days, not 24-hour periods.
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 11, 22, 23, 30, 0, TimeSpan.Zero));
        _policy = new ExchangeRateAgePolicy(_timeProvider, maxAgeInDays: 4);
    }

    [Theory]
    [InlineData(22)]
    [InlineData(20)]
    [InlineData(18)]
    public void IsStale_UpToTheLimit_IsFalse(int day)
    {
        ExchangeRate rate = ExchangeRate.Create(Currency.EUR, Currency.USD, 1.1m, new DateOnly(2026, 11, day));

        Assert.False(_policy.IsStale(rate));
    }

    [Theory]
    [InlineData(17)]
    [InlineData(1)]
    public void IsStale_OlderThanTheLimit_IsTrue(int day)
    {
        ExchangeRate rate = ExchangeRate.Create(Currency.EUR, Currency.USD, 1.1m, new DateOnly(2026, 11, day));

        Assert.True(_policy.IsStale(rate));
    }

    [Fact]
    public void Constructor_WithNegativeLimit_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExchangeRateAgePolicy(_timeProvider, -1));
    }
}