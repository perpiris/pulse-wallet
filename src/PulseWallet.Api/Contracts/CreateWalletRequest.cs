using PulseWallet.Application.Wallets.CreateWallet;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public class CreateWalletRequest
{
    public required string Username { get; init; }
    public required Currency Currency { get; init; }

    public CreateWalletCommand ToCommand()
    {
        return new CreateWalletCommand(Username, Currency);
    }
}