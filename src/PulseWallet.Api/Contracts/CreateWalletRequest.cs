using PulseWallet.Application.Wallets.CreateWallet;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record CreateWalletRequest
{
    public required string Username { get; init; }
    public required Currency Currency { get; init; }

    public CreateWalletCommand ToCommand(Guid ownerId)
    {
        return new CreateWalletCommand(ownerId, Username, Currency);
    }
}