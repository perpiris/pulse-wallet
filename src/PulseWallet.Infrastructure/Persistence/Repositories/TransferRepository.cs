using Microsoft.EntityFrameworkCore;
using PulseWallet.Domain.Transfers;

namespace PulseWallet.Infrastructure.Persistence.Repositories;

public class TransferRepository : ITransferRepository
{
    private readonly PulseWalletDbContext _dbContext;

    public TransferRepository(PulseWalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Transfer?> GetByIdempotencyKeyAsync(Guid sourceWalletId, Guid idempotencyKey, CancellationToken cancellationToken)
    {
        return await _dbContext.Transfers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.SourceWalletId == sourceWalletId && t.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public void Add(Transfer transfer)
    {
        _dbContext.Transfers.Add(transfer);
    }
}