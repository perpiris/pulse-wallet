namespace PulseWallet.Domain.Ledger;

public static class LedgerEntryTypes
{
    private static readonly LedgerEntryType[] Incoming = [LedgerEntryType.TopUp];
    private static readonly LedgerEntryType[] Outgoing = [LedgerEntryType.Withdrawal];

    public static LedgerEntryType[] For(LedgerEntryDirection direction)
    {
        return direction switch
        {
            LedgerEntryDirection.Incoming => Incoming,
            LedgerEntryDirection.Outgoing => Outgoing,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction.")
        };
    }
}