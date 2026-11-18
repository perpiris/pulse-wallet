using PulseWallet.Domain.Common;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Domain.UnitTests.Wallets;

public sealed class WalletTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    [Fact]
    public void Create_WithValidUsername_ReturnsEmptyWalletWithNormalizedUsername()
    {
        Wallet wallet = Wallet.Create(OwnerId, "  Alice_01 ", Currency.EUR, Now);

        Assert.NotEqual(Guid.Empty, wallet.Id);
        Assert.Equal("alice_01", wallet.Username);
        Assert.Equal(Currency.EUR, wallet.Currency);
        Assert.Equal(0m, wallet.Balance);
        Assert.Equal(Now, wallet.CreatedAt);
        Assert.Equal(OwnerId, wallet.OwnerId);
    }

    [Fact]
    public void Create_WithoutOwner_Throws()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Wallet.Create(Guid.Empty, "Alice", Currency.EUR, Now));

        Assert.Contains("owner", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsOwnedBy_ReturnsTrueOnlyForTheOwner()
    {
        Wallet wallet = CreateWallet();

        Assert.True(wallet.IsOwnedBy(OwnerId));
        Assert.False(wallet.IsOwnedBy(Guid.CreateVersion7()));
    }

    [Theory]
    [InlineData("alice_1!")]
    [InlineData("alicé")]
    public void Create_WithInvalidUsernameChar_ThrowsDomainException(string username)
    {
        DomainException exception = Assert.Throws<DomainException>(() => Wallet.Create(OwnerId, username, Currency.EUR, Now));
        Assert.Contains("Username cannot contain special characters", exception.Message);
    }

    [Theory]
    [InlineData("al")]
    [InlineData("alicealicealicealicealicealicealicealicealicealicealice")]
    [InlineData("  al  ")]
    public void Create_WithOurOfRangeUsernameLength_ThrowsDomainException(string username)
    {
        DomainException exception = Assert.Throws<DomainException>(() => Wallet.Create(OwnerId, username, Currency.EUR, Now));
        Assert.Contains("Username must be between", exception.Message);
    }

    [Fact]
    public void Create_WithUnsupportedCurrency_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(() => Wallet.Create(OwnerId, "Alice", (Currency)123, Now));
        Assert.Contains("is not supported", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingUsername_ThrowsDomainException(string? username)
    {
        DomainException exception = Assert.Throws<DomainException>(() => Wallet.Create(OwnerId, username!, Currency.EUR, Now));
        Assert.Contains("Username cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abcdefghijklmnopqrst")]
    public void Create_WithUsernameAtLengthLimit_Succeeds(string username)
    {
        Wallet wallet = Wallet.Create(OwnerId, username, Currency.EUR, Now);

        Assert.Equal(username, wallet.Username);
    }

    public static readonly TheoryData<decimal> ValidTopUpAmounts =
    [
        50m,
        50.10m,
        0.01m
    ];

    [Theory]
    [MemberData(nameof(ValidTopUpAmounts))]
    public void TopUp_WithValidAmount_IncreasesBalanceAndReturnsTopUpEntry(decimal topUpAmount)
    {
        Wallet wallet = CreateWallet();
        Money amount = Money.Create(topUpAmount, Currency.EUR);
        Guid idempotencyKey = Guid.NewGuid();

        LedgerEntry entry = wallet.TopUp(amount, idempotencyKey, Now);

        Assert.Equal(topUpAmount, wallet.Balance);
        Assert.Equal(LedgerEntryType.TopUp, entry.Type);
        Assert.Equal(wallet.Id, entry.WalletId);
        Assert.Equal(amount, entry.Amount);
        Assert.Equal(idempotencyKey, entry.IdempotencyKey);
        Assert.Equal(Now, entry.CreatedAt);
        Assert.Null(entry.TransferId);
    }

    public static readonly TheoryData<decimal> ValidWithdrawalAmounts =
    [
        50m,
        50.10m,
        0.01m,
        1000m
    ];

    [Theory]
    [MemberData(nameof(ValidWithdrawalAmounts))]
    public void Withdraw_WithValidAmount_DecreasesBalanceAndReturnsWithdrawalEntry(decimal withdrawalAmount)
    {
        Wallet wallet = CreateWalletWithBalance(1000m);
        Money amount = Money.Create(withdrawalAmount, Currency.EUR);
        Guid idempotencyKey = Guid.NewGuid();

        LedgerEntry entry = wallet.Withdraw(amount, idempotencyKey, Now);

        Assert.Equal(1000m - withdrawalAmount, wallet.Balance);
        Assert.Equal(LedgerEntryType.Withdrawal, entry.Type);
        Assert.Equal(wallet.Id, entry.WalletId);
        Assert.Equal(amount, entry.Amount);
        Assert.Equal(idempotencyKey, entry.IdempotencyKey);
        Assert.Equal(Now, entry.CreatedAt);
        Assert.Null(entry.TransferId);
    }

    [Fact]
    public void TopUp_WithDifferentCurrency_ThrowsDomainExceptionAndKeepsBalance()
    {
        Wallet wallet = CreateWalletWithBalance(100m);

        DomainException exception = Assert.Throws<DomainException>(
            () => wallet.TopUp(Money.Create(50m, Currency.USD), Guid.NewGuid(), Now));

        Assert.Contains("Wallet currency is EUR, but the amount is in USD", exception.Message);
        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void TopUp_WithZeroAmount_ThrowsDomainExceptionAndKeepsBalance()
    {
        Wallet wallet = CreateWalletWithBalance(100m);

        DomainException exception = Assert.Throws<DomainException>(
            () => wallet.TopUp(Money.Create(0m, Currency.EUR), Guid.NewGuid(), Now));

        Assert.Contains("Amount must be greater than zero", exception.Message);
        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_WholeBalance_LeavesZeroBalance()
    {
        Wallet wallet = CreateWalletWithBalance(100m);

        wallet.Withdraw(Money.Create(100m, Currency.EUR), Guid.NewGuid(), Now);

        Assert.Equal(0m, wallet.Balance);
    }

    public static readonly TheoryData<decimal> AmountsAboveBalanceOf100 =
    [
        100.01m,
        500m
    ];

    [Theory]
    [MemberData(nameof(AmountsAboveBalanceOf100))]
    public void Withdraw_MoreThanBalance_ThrowsInsufficientFundsExceptionAndKeepsBalance(decimal requested)
    {
        Wallet wallet = CreateWalletWithBalance(100m);
        Money amount = Money.Create(requested, Currency.EUR);

        Assert.Throws<InsufficientFundsException>(() => wallet.Withdraw(amount, Guid.NewGuid(), Now));

        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_WithDifferentCurrency_ThrowsDomainExceptionAndKeepsBalance()
    {
        Wallet wallet = CreateWalletWithBalance(100m);

        DomainException exception = Assert.Throws<DomainException>(
            () => wallet.Withdraw(Money.Create(50m, Currency.USD), Guid.NewGuid(), Now));

        Assert.Contains("Wallet currency is EUR, but the amount is in USD", exception.Message);
        Assert.Equal(100m, wallet.Balance);
    }

    [Fact]
    public void Withdraw_WithZeroAmount_ThrowsDomainExceptionAndKeepsBalance()
    {
        Wallet wallet = CreateWalletWithBalance(100m);

        DomainException exception = Assert.Throws<DomainException>(
            () => wallet.Withdraw(Money.Create(0m, Currency.EUR), Guid.NewGuid(), Now));

        Assert.Contains("Amount must be greater than zero", exception.Message);
        Assert.Equal(100m, wallet.Balance);
    }

    private static Wallet CreateWallet(Currency currency = Currency.EUR)
    {
        return Wallet.Create(OwnerId, "Alice", currency, Now);
    }

    private static Wallet CreateWalletWithBalance(decimal balance, Currency currency = Currency.EUR)
    {
        Wallet wallet = CreateWallet(currency);
        wallet.TopUp(Money.Create(balance, currency), Guid.NewGuid(), Now);
        return wallet;
    }
}