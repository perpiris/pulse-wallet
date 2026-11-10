namespace PulseWallet.Application.Wallets;

/// <summary>
/// The read side for wallets (CQRS-lite): returns read models straight from the database,
/// without loading aggregates. Writes still go through IWalletRepository.
/// </summary>
public interface IWalletQueries
{
    Task<WalletDetails?> GetDetailsAsync(Guid walletId, int recentEntries, CancellationToken cancellationToken);
}