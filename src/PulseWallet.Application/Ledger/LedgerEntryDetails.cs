using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Ledger;

// Money is flattened into Amount + Currency: the read model is plain data, not a domain object.
public record LedgerEntryDetails(Guid Id, LedgerEntryType Type, decimal Amount, Currency Currency, DateTimeOffset CreatedAt);