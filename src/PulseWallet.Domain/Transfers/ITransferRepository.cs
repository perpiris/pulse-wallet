namespace PulseWallet.Domain.Transfers;

public interface ITransferRepository
{
    Task<Transfer?> GetByIdempotencyKeyAsync(Guid sourceWalletId, Guid idempotencyKey, CancellationToken cancellationToken);
    void Add(Transfer transfer);
}