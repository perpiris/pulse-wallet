using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Wallets.TopUpWallet;

public record TopUpWalletCommand(Guid WalletId, Guid UserId, decimal Amount, Currency Currency, Guid IdempotencyKey);