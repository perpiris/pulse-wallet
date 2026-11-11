using PulseWallet.Application.Wallets.WithdrawFromWallet;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record WithdrawFromWalletRequest
{
    public required decimal Amount { get; init; }
    public required Currency Currency { get; init; }

    public WithdrawFromWalletCommand ToCommand(Guid walletId)
    {
        return new WithdrawFromWalletCommand(walletId, Amount, Currency);
    }
}