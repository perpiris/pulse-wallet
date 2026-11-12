using Microsoft.EntityFrameworkCore;
using PulseWallet.Application.Transfers;

namespace PulseWallet.Infrastructure.Persistence.Queries;

internal sealed class TransferQueries : ITransferQueries
{
    private readonly PulseWalletDbContext _dbContext;

    public TransferQueries(PulseWalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TransferDetails?> GetDetailsAsync(Guid transferId, CancellationToken cancellationToken)
    {
        return await _dbContext.Transfers
            .Where(t => t.Id == transferId)
            .Select(t => new TransferDetails(
                t.Id,
                t.SourceWalletId,
                t.DestinationWalletId,
                t.Amount.Amount,
                t.Amount.Currency,
                t.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}