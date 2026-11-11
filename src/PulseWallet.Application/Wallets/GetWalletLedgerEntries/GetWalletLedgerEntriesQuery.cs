using PulseWallet.Application.Ledger;
using PulseWallet.Domain.Ledger;

namespace PulseWallet.Application.Wallets.GetWalletLedgerEntries;

public record GetWalletLedgerEntriesQuery(
    Guid WalletId,
    DateOnly? From,
    DateOnly? To,
    LedgerEntryDirection? Direction,
    LedgerEntriesCursor? Before);