using PulseWallet.Domain.Common;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Domain.Ledger;

public sealed class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public LedgerEntryType Type { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private LedgerEntry() { }

    public static LedgerEntry Create(Guid walletId, LedgerEntryType type, Money amount, DateTimeOffset now)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException($"Ledger entry type '{type}' is not supported.");
        }

        if (amount.Amount <= 0)
        {
            throw new DomainException("Amount must be greater than zero.");
        }

        return new LedgerEntry
        {
            Id = Guid.CreateVersion7(),
            WalletId = walletId,
            Type = type,
            Amount = amount,
            CreatedAt = now
        };
    }
}