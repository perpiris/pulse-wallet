using PulseWallet.Application.Wallets;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record WalletDetailsResponse(
    Guid Id,
    string Username,
    Currency Currency,
    decimal Balance,
    DateTimeOffset CreatedAt,
    IReadOnlyList<LedgerEntryResponse> RecentEntries)
{
    public static WalletDetailsResponse From(WalletDetails details)
    {
        return new WalletDetailsResponse(
            details.Id,
            details.Username,
            details.Currency,
            details.Balance,
            details.CreatedAt,
            details.RecentEntries.Select(LedgerEntryResponse.From).ToList());
    }
}