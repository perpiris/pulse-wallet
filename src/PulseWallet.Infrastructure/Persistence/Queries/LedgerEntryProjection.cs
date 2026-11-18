using System.Linq.Expressions;
using PulseWallet.Application.Ledger;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Transfers;
using PulseWallet.Domain.Wallets;
using PulseWallet.Infrastructure.Identity;

namespace PulseWallet.Infrastructure.Persistence.Queries;

// Here rather than in Application: the counterparty's email comes from the Identity users table, which only
// Infrastructure knows about. An expression, so EF turns it into SQL (subqueries per row), not C# after loading.
internal static class LedgerEntryProjection
{
    // The counterparty is the other wallet of the transfer, seen from walletId, and then that wallet's owner.
    public static Expression<Func<LedgerEntry, LedgerEntryDetails>> For(
        Guid walletId,
        IQueryable<Transfer> transfers,
        IQueryable<Wallet> wallets,
        IQueryable<ApplicationUser> users)
    {
        return e => new LedgerEntryDetails(
            e.Id,
            e.Type,
            e.Amount.Amount,
            e.Amount.Currency,
            e.CreatedAt,
            e.TransferId,
            transfers
                .Where(t => t.Id == e.TransferId)
                .Join(
                    wallets,
                    t => t.SourceWalletId == walletId ? t.DestinationWalletId : t.SourceWalletId,
                    w => w.Id,
                    (t, w) => w.OwnerId)
                .Join(users, ownerId => ownerId, u => u.Id, (ownerId, u) => u.Email)
                .FirstOrDefault());
    }
}