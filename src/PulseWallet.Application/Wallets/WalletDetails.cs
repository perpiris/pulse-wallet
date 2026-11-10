using PulseWallet.Application.Ledger;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Wallets;

public record WalletDetails(
    Guid Id,
    string Username,
    Currency Currency,
    decimal Balance,
    DateTimeOffset CreatedAt,
    IReadOnlyList<LedgerEntryDetails> RecentEntries)
{
    // How many of the latest ledger entries the wallet details include. The full history gets its own paged endpoint.
    public const int RecentEntriesCount = 10;
}