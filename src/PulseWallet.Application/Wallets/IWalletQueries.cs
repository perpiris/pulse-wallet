using PulseWallet.Application.Ledger;

namespace PulseWallet.Application.Wallets;

public interface IWalletQueries
{
    // Both return null for a wallet that doesn't exist or isn't owned by ownerId: the owner filter is part of the
    // query itself, so a read can never return someone else's wallet, and callers can't forget the check.
    Task<WalletDetails?> GetDetailsAsync(Guid walletId, Guid ownerId, int recentEntries, CancellationToken cancellationToken);

    Task<IReadOnlyList<WalletSummary>> ListOwnedAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<LedgerEntriesPage?> GetLedgerEntriesAsync(
        Guid walletId,
        Guid ownerId,
        LedgerEntriesFilter filter,
        CancellationToken cancellationToken);
}