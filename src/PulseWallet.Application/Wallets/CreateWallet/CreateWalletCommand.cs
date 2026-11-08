using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Wallets.CreateWallet;

public record CreateWalletCommand(string Username, Currency Currency);