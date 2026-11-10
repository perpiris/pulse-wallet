namespace PulseWallet.Application.Wallets.GetWalletById;

public class GetWalletByIdHandler
{
    private readonly IWalletQueries _walletQueries;

    public GetWalletByIdHandler(IWalletQueries walletQueries)
    {
        _walletQueries = walletQueries;
    }

    public async Task<WalletDetails> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var wallet = await _walletQueries.GetDetailsAsync(id, WalletDetails.RecentEntriesCount, cancellationToken);

        if (wallet is null)
        {
            throw new WalletNotFoundException(id);
        }

        return wallet;
    }
}