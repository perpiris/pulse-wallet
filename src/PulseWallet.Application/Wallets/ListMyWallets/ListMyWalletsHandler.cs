namespace PulseWallet.Application.Wallets.ListMyWallets;

public sealed class ListMyWalletsHandler
{
    private readonly IWalletQueries _walletQueries;

    public ListMyWalletsHandler(IWalletQueries walletQueries)
    {
        _walletQueries = walletQueries;
    }

    public async Task<IReadOnlyList<WalletSummary>> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _walletQueries.ListOwnedAsync(userId, cancellationToken);
    }
}