using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Domain.UnitTests.Ledger;

public sealed class LedgerEntryTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    [Fact]
    public void Matches_WithSameTypeAndAmount_IsTrue()
    {
        LedgerEntry entry = CreateTopUpEntry(50m);

        Assert.True(entry.Matches(LedgerEntryType.TopUp, Money.Create(50m, Currency.EUR)));
    }

    [Fact]
    public void Matches_WithDifferentType_IsFalse()
    {
        LedgerEntry entry = CreateTopUpEntry(50m);

        Assert.False(entry.Matches(LedgerEntryType.Withdrawal, Money.Create(50m, Currency.EUR)));
    }

    [Fact]
    public void Matches_WithDifferentAmount_IsFalse()
    {
        LedgerEntry entry = CreateTopUpEntry(50m);

        Assert.False(entry.Matches(LedgerEntryType.TopUp, Money.Create(50.01m, Currency.EUR)));
    }

    private static LedgerEntry CreateTopUpEntry(decimal amount)
    {
        Wallet wallet = Wallet.Create(OwnerId, "alice", Currency.EUR, Now);
        return wallet.TopUp(Money.Create(amount, Currency.EUR), Guid.NewGuid(), Now);
    }
}