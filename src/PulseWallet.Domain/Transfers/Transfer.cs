using PulseWallet.Domain.Common;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Domain.Transfers;

public sealed class Transfer
{
    public Guid Id { get; private set; }
    public Guid SourceWalletId { get; private set; }
    public Guid DestinationWalletId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    // Unique per source wallet: the sender's retry with the same key finds this transfer instead of sending again.
    public Guid IdempotencyKey { get; private set; }

    private Transfer() { }

    public bool Matches(Guid destinationWalletId, Money amount)
    {
        return DestinationWalletId == destinationWalletId && Amount == amount;
    }

    // Changes two aggregates in one transaction, on purpose: an instant transfer must never be half done,
    // and both wallets are in the same database with their own row versions.
    public static TransferExecution Execute(
        Wallet source,
        Wallet destination,
        Money amount,
        Guid idempotencyKey,
        DateTimeOffset now)
    {
        if (source.Id == destination.Id)
        {
            throw new DomainException("A wallet cannot transfer to itself.");
        }

        if (source.Currency != destination.Currency)
        {
            throw new DomainException(
                $"Cross-currency transfers are not supported yet ({source.Currency} to {destination.Currency}).");
        }

        Transfer transfer = new Transfer
        {
            Id = Guid.CreateVersion7(),
            SourceWalletId = source.Id,
            DestinationWalletId = destination.Id,
            Amount = amount,
            IdempotencyKey = idempotencyKey,
            CreatedAt = now
        };

        LedgerEntry debit = source.SendTransfer(transfer.Id, amount, now);
        LedgerEntry credit = destination.ReceiveTransfer(transfer.Id, amount, now);

        return new TransferExecution(transfer, debit, credit);
    }
}