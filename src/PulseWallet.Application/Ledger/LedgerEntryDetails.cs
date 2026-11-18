using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Ledger;

// Money is flattened into Amount + Currency: the read model is plain data, not a domain object.
// CounterpartyEmail: for a transfer, the email of the user who owns the other wallet (the other wallet's
// friendly name is that user's private label, so it isn't shown); null for top-ups and withdrawals.
// How it's filled from the database lives in Infrastructure (LedgerEntryProjection), next to the user table.
public record LedgerEntryDetails(
    Guid Id,
    LedgerEntryType Type,
    decimal Amount,
    Currency Currency,
    DateTimeOffset CreatedAt,
    Guid? TransferId,
    string? CounterpartyEmail);