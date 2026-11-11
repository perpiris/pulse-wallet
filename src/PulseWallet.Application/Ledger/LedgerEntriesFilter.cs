using PulseWallet.Domain.Ledger;

namespace PulseWallet.Application.Ledger;

public record LedgerEntriesFilter(
    DateTimeOffset? From,
    DateTimeOffset? ToExclusive,
    LedgerEntryDirection? Direction,
    LedgerEntriesCursor? Before,
    int PageSize);