namespace PulseWallet.Application.Wallets.GetWalletById;

public class WalletNotFoundException(Guid walletId)
    : Exception($"Wallet '{walletId}' was not found.");