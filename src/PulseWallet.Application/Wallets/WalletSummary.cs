using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Wallets;

// One row of "my wallets": just enough for a list, without the ledger entries WalletDetails carries.
public record WalletSummary(
    Guid Id,
    string FriendlyName,
    Currency Currency,
    decimal Balance,
    DateTimeOffset CreatedAt);