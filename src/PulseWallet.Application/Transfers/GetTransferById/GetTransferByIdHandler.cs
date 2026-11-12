namespace PulseWallet.Application.Transfers.GetTransferById;

public sealed class GetTransferByIdHandler
{
    private readonly ITransferQueries _transferQueries;

    public GetTransferByIdHandler(ITransferQueries transferQueries)
    {
        _transferQueries = transferQueries;
    }

    public async Task<TransferDetails> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        TransferDetails? transfer = await _transferQueries.GetDetailsAsync(id, cancellationToken);

        if (transfer is null)
        {
            throw new TransferNotFoundException(id);
        }

        return transfer;
    }
}