using System.ComponentModel.DataAnnotations;
using PulseWallet.Application.Ledger;
using PulseWallet.Application.Wallets.GetWalletLedgerEntries;
using PulseWallet.Domain.Ledger;

namespace PulseWallet.Api.Contracts;

public record GetWalletLedgerEntriesRequest : IValidatableObject
{
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public LedgerEntryDirection? Direction { get; init; }

    public string? Before { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From is not null && To is not null && From > To)
        {
            yield return new ValidationResult("'from' must be on or before 'to'.", [nameof(From), nameof(To)]);
        }

        if (Before is not null && !LedgerEntriesCursor.TryDecode(Before, out _))
        {
            yield return new ValidationResult("'before' is not a valid cursor.", [nameof(Before)]);
        }
    }

    public GetWalletLedgerEntriesQuery ToQuery(Guid walletId, Guid userId)
    {
        LedgerEntriesCursor.TryDecode(Before, out LedgerEntriesCursor? before);
        return new GetWalletLedgerEntriesQuery(walletId, userId, From, To, Direction, before);
    }
}