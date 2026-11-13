using PulseWallet.Domain.Common;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Domain.UnitTests.Shared;

public sealed class MoneyTests
{
    public static TheoryData<decimal> ValidAmounts =
    [
        0m,
        0.01m,
        100.50m
    ];

    [Theory]
    [MemberData(nameof(ValidAmounts))]
    public void Create_WithValidAmount_ReturnsMoney(decimal value)
    {
        Money money = Money.Create(value, Currency.EUR);

        Assert.Equal(value, money.Amount);
        Assert.Equal(Currency.EUR, money.Currency);
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Money.Create(-0.01m, Currency.EUR));
        Assert.Contains("Amount cannot be negative", exception.Message);
    }

    public static TheoryData<decimal> AmountsWithMoreThanTwoDecimals =
    [
        0.001m,
        10.555m
    ];

    [Theory]
    [MemberData(nameof(AmountsWithMoreThanTwoDecimals))]
    public void Create_WithMoreThanTwoDecimals_ThrowsDomainException(decimal value)
    {
        DomainException exception = Assert.Throws<DomainException>(() => Money.Create(value, Currency.EUR));
        Assert.Contains("has more than 2 decimal places", exception.Message);
    }

    [Fact]
    public void Create_WithUnsupportedCurrency_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Money.Create(10m, (Currency)99));
        Assert.Contains("is not supported", exception.Message);
    }

    [Fact]
    public void Equals_WithSameAmountAndCurrency_IsTrue()
    {
        Money first = Money.Create(10.5m, Currency.EUR);
        Money second = Money.Create(10.50m, Currency.EUR);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_WithDifferentCurrency_IsFalse()
    {
        Money euros = Money.Create(10m, Currency.EUR);
        Money dollars = Money.Create(10m, Currency.USD);

        Assert.NotEqual(euros, dollars);
    }

    [Fact]
    public void Equals_WithDifferentAmount_IsFalse()
    {
        Money ten = Money.Create(10m, Currency.EUR);
        Money tenAndOneCent = Money.Create(10.01m, Currency.EUR);

        Assert.NotEqual(ten, tenAndOneCent);
    }
}