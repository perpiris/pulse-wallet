using System.Linq.Expressions;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Transfers;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.Ledger;

// Money is flattened into Amount + Currency: the read model is plain data, not a domain object.
public record LedgerEntryDetails(
    Guid Id,
    LedgerEntryType Type,
    decimal Amount,
    Currency Currency,
    DateTimeOffset CreatedAt,
    Guid? TransferId,
    string? CounterpartyUsername)
{
    // The counterparty is the other wallet of the transfer, seen from walletId; null for top-ups and withdrawals.
    public static Expression<Func<LedgerEntry, LedgerEntryDetails>> Projection(
        Guid walletId,
        IQueryable<Transfer> transfers,
        IQueryable<Wallet> wallets)
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
                    (t, w) => w.Username)
                .FirstOrDefault());
    }
}