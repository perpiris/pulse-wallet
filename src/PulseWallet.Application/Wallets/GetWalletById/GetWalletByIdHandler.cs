using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.Wallets.GetWalletById;

public class GetWalletByIdHandler
{
    private readonly IWalletRepository _walletRepository;

    public GetWalletByIdHandler(IWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public async Task<WalletDetails> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepository.GetByIdAsync(id, cancellationToken);

        if (wallet is null)
        {
            throw new WalletNotFoundException(id);
        }

        return WalletDetails.From(wallet);
    }
}