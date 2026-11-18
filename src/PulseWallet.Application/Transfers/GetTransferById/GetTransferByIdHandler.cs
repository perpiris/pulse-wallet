namespace PulseWallet.Application.Transfers.GetTransferById;

public sealed class GetTransferByIdHandler
{
    private readonly ITransferQueries _transferQueries;

    public GetTransferByIdHandler(ITransferQueries transferQueries)
    {
        _transferQueries = transferQueries;
    }

    public async Task<TransferDetails> HandleAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        TransferDetails? transfer = await _transferQueries.GetDetailsAsync(id, userId, cancellationToken);

        if (transfer is null)
        {
            throw new TransferNotFoundException(id);
        }

        return transfer;
    }
}