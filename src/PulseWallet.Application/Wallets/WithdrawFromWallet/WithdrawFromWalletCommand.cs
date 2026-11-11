using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Wallets.WithdrawFromWallet;

public record WithdrawFromWalletCommand(Guid WalletId, decimal Amount, Currency Currency, Guid IdempotencyKey);
