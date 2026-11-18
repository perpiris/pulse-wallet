using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Wallets.CreateWallet;

public record CreateWalletCommand(Guid OwnerId, string Username, Currency Currency);