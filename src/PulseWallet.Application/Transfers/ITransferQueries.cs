namespace PulseWallet.Application.Transfers;

public interface ITransferQueries
{
    // Null when the transfer doesn't exist or the user owns neither of its wallets (sender and receiver can both see it).
    Task<TransferDetails?> GetDetailsAsync(Guid transferId, Guid userId, CancellationToken cancellationToken);
}