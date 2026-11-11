using PulseWallet.Application.Ledger;

namespace PulseWallet.Api.Contracts;

public record LedgerEntriesPageResponse(IReadOnlyList<LedgerEntryResponse> Items, string? NextCursor)
{
    public static LedgerEntriesPageResponse From(LedgerEntriesPage page)
    {
        return new LedgerEntriesPageResponse(
            page.Items.Select(LedgerEntryResponse.From).ToList(),
            page.NextCursor);
    }
}