using PulseWallet.Application.Wallets.WithdrawFromWallet;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record WithdrawFromWalletRequest
{
    public required decimal Amount { get; init; }
    public required Currency Currency { get; init; }

    public WithdrawFromWalletCommand ToCommand(Guid walletId, Guid userId, Guid idempotencyKey)
    {
        return new WithdrawFromWalletCommand(walletId, userId, Amount, Currency, idempotencyKey);
    }
}