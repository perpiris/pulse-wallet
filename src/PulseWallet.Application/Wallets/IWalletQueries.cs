using PulseWallet.Application.Ledger;

namespace PulseWallet.Application.Wallets;

public interface IWalletQueries
{
    Task<WalletDetails?> GetDetailsAsync(Guid walletId, int recentEntries, CancellationToken cancellationToken);
    Task<LedgerEntriesPage?> GetLedgerEntriesAsync(Guid walletId, LedgerEntriesFilter filter, CancellationToken cancellationToken);
}