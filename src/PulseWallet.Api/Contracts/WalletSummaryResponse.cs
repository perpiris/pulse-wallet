using PulseWallet.Application.Wallets;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record WalletSummaryResponse(
    Guid Id,
    string Username,
    Currency Currency,
    decimal Balance,
    DateTimeOffset CreatedAt)
{
    public static WalletSummaryResponse From(WalletSummary summary)
    {
        return new WalletSummaryResponse(
            summary.Id,
            summary.Username,
            summary.Currency,
            summary.Balance,
            summary.CreatedAt);
    }
}