using PulseWallet.Domain.Ledger;

namespace PulseWallet.Domain.UnitTests.Ledger;

public sealed class LedgerEntryTypesTests
{
    [Fact]
    public void For_Incoming_ReturnsTopUpAndTransferIn()
    {
        LedgerEntryType[] types = LedgerEntryTypes.For(LedgerEntryDirection.Incoming);

        Assert.Equal([LedgerEntryType.TopUp, LedgerEntryType.TransferIn], types);
    }

    [Fact]
    public void For_Outgoing_ReturnsWithdrawalAndTransferOut()
    {
        LedgerEntryType[] types = LedgerEntryTypes.For(LedgerEntryDirection.Outgoing);

        Assert.Equal([LedgerEntryType.Withdrawal, LedgerEntryType.TransferOut], types);
    }

    [Fact]
    public void For_EveryTypeBelongsToExactlyOneDirection()
    {
        // Fails when a new LedgerEntryType is added without deciding whether it's incoming or outgoing.
        LedgerEntryType[] incoming = LedgerEntryTypes.For(LedgerEntryDirection.Incoming);
        LedgerEntryType[] outgoing = LedgerEntryTypes.For(LedgerEntryDirection.Outgoing);

        foreach (LedgerEntryType type in Enum.GetValues<LedgerEntryType>())
        {
            Assert.True(incoming.Contains(type) ^ outgoing.Contains(type), $"{type} must be in exactly one direction.");
        }
    }

    [Fact]
    public void For_WithUndefinedDirection_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LedgerEntryTypes.For((LedgerEntryDirection)99));
    }
}