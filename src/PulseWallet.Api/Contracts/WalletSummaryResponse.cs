using PulseWallet.Application.Wallets;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record WalletSummaryResponse(
    Guid Id,
    string FriendlyName,
    Currency Currency,
    decimal Balance,
    DateTimeOffset CreatedAt)
{
    public static WalletSummaryResponse From(WalletSummary summary)
    {
        return new WalletSummaryResponse(
            summary.Id,
            summary.FriendlyName,
            summary.Currency,
            summary.Balance,
            summary.CreatedAt);
    }
}