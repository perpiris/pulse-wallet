using PulseWallet.Domain.Common;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Domain.UnitTests.Shared;

public sealed class ExchangeRateTests
{
    private static readonly DateOnly RateDate = new(2026, 11, 20);

    [Fact]
    public void Create_WithValidRate_ReturnsRate()
    {
        ExchangeRate rate = ExchangeRate.Create(Currency.EUR, Currency.DKK, 7.46012345m, RateDate);

        Assert.Equal(Currency.EUR, rate.From);
        Assert.Equal(Currency.DKK, rate.To);
        Assert.Equal(7.46012345m, rate.Rate);
        Assert.Equal(RateDate, rate.Date);
    }

    [Fact]
    public void Create_WithSameCurrencyTwice_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ExchangeRate.Create(Currency.EUR, Currency.EUR, 1m, RateDate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void Create_WithRateNotAboveZero_ThrowsDomainException(decimal value)
    {
        Assert.Throws<DomainException>(() => ExchangeRate.Create(Currency.EUR, Currency.USD, value, RateDate));
    }

    [Fact]
    public void Create_WithMoreThanEightDecimals_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ExchangeRate.Create(Currency.EUR, Currency.USD, 1.123456789m, RateDate));
    }

    [Fact]
    public void Create_WithUnsupportedCurrency_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ExchangeRate.Create(Currency.EUR, (Currency)99, 1m, RateDate));
    }

    [Theory]
    [InlineData(10, 1.1, 11)]
    [InlineData(10, 1.0999, 10.99)]
    [InlineData(0.01, 0.99, 0)]
    [InlineData(99.99, 7.4699, 746.91)]
    public void Convert_RoundsDownToCents(decimal amount, decimal rateValue, decimal expected)
    {
        ExchangeRate rate = ExchangeRate.Create(Currency.EUR, Currency.USD, rateValue, RateDate);

        Money converted = rate.Convert(Money.Create(amount, Currency.EUR));

        Assert.Equal(Money.Create(expected, Currency.USD), converted);
    }

    [Fact]
    public void Convert_AmountInWrongCurrency_ThrowsDomainException()
    {
        ExchangeRate rate = ExchangeRate.Create(Currency.EUR, Currency.USD, 1.1m, RateDate);

        Assert.Throws<DomainException>(() => rate.Convert(Money.Create(10m, Currency.DKK)));
    }
}