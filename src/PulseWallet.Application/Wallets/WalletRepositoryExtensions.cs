using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.Wallets;

public static class WalletRepositoryExtensions
{
    // Resource-based authorization for commands: the decision needs the wallet itself, so it's made where the
    // wallet is loaded. Someone else's wallet gets the same answer as a missing one (404, not 403):
    // a 403 would confirm that the id exists, letting anyone probe for other people's wallets.
    public static async Task<Wallet> GetOwnedAsync(
        this IWalletRepository walletRepository,
        Guid walletId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        Wallet? wallet = await walletRepository.GetByIdAsync(walletId, cancellationToken);
        if (wallet is null || !wallet.IsOwnedBy(userId))
        {
            throw new WalletNotFoundException(walletId);
        }

        return wallet;
    }
}
