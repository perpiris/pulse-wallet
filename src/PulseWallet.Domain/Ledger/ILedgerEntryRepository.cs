namespace PulseWallet.Domain.Ledger;

public interface ILedgerEntryRepository
{
    Task<LedgerEntry?> GetByIdempotencyKeyAsync(Guid walletId, Guid idempotencyKey, CancellationToken cancellationToken);
    void Add(LedgerEntry entry);
}
