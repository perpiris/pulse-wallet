using PulseWallet.Application.Wallets.TopUpWallet;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record TopUpWalletRequest
{
    public required decimal Amount { get; init; }
    public required Currency Currency { get; init; }

    public TopUpWalletCommand ToCommand(Guid walletId, Guid userId, Guid idempotencyKey)
    {
        return new TopUpWalletCommand(walletId, userId, Amount, Currency, idempotencyKey);
    }
}