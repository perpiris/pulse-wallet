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

    // The client's Idempotency-Key for the request that created this entry: a retry with the same key
    // finds this entry instead of moving money again. Null for entries created by a transfer
    // (the key of a transfer belongs on the Transfer).
    public Guid? IdempotencyKey { get; private set; }

    // Set on the two entries of a transfer (TransferOut on the source, TransferIn on the destination).
    public Guid? TransferId { get; private set; }

    private LedgerEntry() { }

    public bool Matches(LedgerEntryType type, Money amount)
    {
        return Type == type && Amount == amount;
    }

    public static LedgerEntry Create(
        Guid walletId,
        LedgerEntryType type,
        Money amount,
        Guid? idempotencyKey,
        DateTimeOffset now)
    {
        if (type is LedgerEntryType.TransferIn or LedgerEntryType.TransferOut)
        {
            throw new DomainException($"Ledger entry type '{type}' needs a transfer.");
        }

        return Create(walletId, type, amount, idempotencyKey, transferId: null, now);
    }

    public static LedgerEntry CreateForTransfer(
        Guid walletId,
        LedgerEntryType type,
        Money amount,
        Guid transferId,
        DateTimeOffset now)
    {
        if (type is not (LedgerEntryType.TransferIn or LedgerEntryType.TransferOut))
        {
            throw new DomainException($"Ledger entry type '{type}' is not a transfer type.");
        }

        return Create(walletId, type, amount, idempotencyKey: null, transferId, now);
    }

    private static LedgerEntry Create(
        Guid walletId,
        LedgerEntryType type,
        Money amount,
        Guid? idempotencyKey,
        Guid? transferId,
        DateTimeOffset now)
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
            IdempotencyKey = idempotencyKey,
            TransferId = transferId,
            CreatedAt = now
        };
    }
}