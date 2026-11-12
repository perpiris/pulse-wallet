namespace PulseWallet.Application.Transfers;

public interface ITransferQueries
{
    Task<TransferDetails?> GetDetailsAsync(Guid transferId, CancellationToken cancellationToken);
}