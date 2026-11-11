namespace PulseWallet.Application.Ledger;

public record LedgerEntriesPage(IReadOnlyList<LedgerEntryDetails> Items, string? NextCursor);