namespace PulseWallet.Application.Wallets;

public class WalletNotFoundException(Guid walletId)
    : Exception($"Wallet '{walletId}' was not found.");