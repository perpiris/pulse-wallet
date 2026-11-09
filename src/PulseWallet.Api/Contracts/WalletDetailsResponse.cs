using PulseWallet.Application.Wallets;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record WalletDetailsResponse(
    Guid Id,
    string Username,
    Currency Currency,
    decimal Balance,
    DateTimeOffset CreatedAt)
{
    public static WalletDetailsResponse From(WalletDetails details) =>
        new(details.Id, details.Username, details.Currency, details.Balance, details.CreatedAt);
}