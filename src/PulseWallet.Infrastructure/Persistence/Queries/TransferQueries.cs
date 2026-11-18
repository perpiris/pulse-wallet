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

    public async Task<TransferDetails?> GetDetailsAsync(Guid transferId, Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Transfers
            .Where(t => t.Id == transferId)
            // Visible to the owners of both sides: an EXISTS subquery on Wallets, still one SQL statement.
            .Where(t => _dbContext.Wallets.Any(w =>
                (w.Id == t.SourceWalletId || w.Id == t.DestinationWalletId) && w.OwnerId == userId))
            .Select(t => new TransferDetails(
                t.Id,
                t.SourceWalletId,
                t.DestinationWalletId,
                t.Amount.Amount,
                t.Amount.Currency,
                t.ReceivedAmount.Amount,
                t.ReceivedAmount.Currency,
                t.Rate,
                t.RateDate,
                t.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}