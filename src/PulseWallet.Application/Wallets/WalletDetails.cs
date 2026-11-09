using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.Wallets;

public record WalletDetails(Guid Id, string Username, Currency Currency, decimal Balance, DateTimeOffset CreatedAt)
{
    public static WalletDetails From(Wallet wallet) =>
        new(wallet.Id, wallet.Username, wallet.Currency, wallet.Balance, wallet.CreatedAt);
}