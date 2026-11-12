using PulseWallet.Application.Ledger;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record LedgerEntryResponse(
    Guid Id,
    LedgerEntryType Type,
    decimal Amount,
    Currency Currency,
    DateTimeOffset CreatedAt,
    Guid? TransferId,
    string? CounterpartyUsername)
{
    public static LedgerEntryResponse From(LedgerEntryDetails details)
    {
        return new LedgerEntryResponse(
            details.Id,
            details.Type,
            details.Amount,
            details.Currency,
            details.CreatedAt,
            details.TransferId,
            details.CounterpartyUsername);
    }
}