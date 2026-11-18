using PulseWallet.Application.Wallets;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record WalletDetailsResponse(
    Guid Id,
    string FriendlyName,
    Currency Currency,
    decimal Balance,
    DateTimeOffset CreatedAt,
    IReadOnlyList<LedgerEntryResponse> RecentEntries)
{
    public static WalletDetailsResponse From(WalletDetails details)
    {
        return new WalletDetailsResponse(
            details.Id,
            details.FriendlyName,
            details.Currency,
            details.Balance,
            details.CreatedAt,
            details.RecentEntries.Select(LedgerEntryResponse.From).ToList());
    }
}