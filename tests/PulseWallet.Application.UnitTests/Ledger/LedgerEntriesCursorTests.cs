using System.Buffers.Text;
using System.Text;
using PulseWallet.Application.Ledger;

namespace PulseWallet.Application.UnitTests.Ledger;

public sealed class LedgerEntriesCursorTests
{
    [Fact]
    public void Encode_ThenTryDecode_ReturnsTheSameCursor()
    {
        LedgerEntriesCursor cursor = new LedgerEntriesCursor(
            new DateTimeOffset(2026, 11, 21, 10, 30, 15, 123, TimeSpan.Zero), Guid.CreateVersion7());

        bool decoded = LedgerEntriesCursor.TryDecode(cursor.Encode(), out LedgerEntriesCursor? result);

        Assert.True(decoded);
        Assert.Equal(cursor, result);
    }

    [Fact]
    public void Encode_WithNonUtcOffset_DecodesToTheSameInstantInUtc()
    {
        DateTimeOffset copenhagen = new DateTimeOffset(2026, 11, 21, 11, 0, 0, TimeSpan.FromHours(1));
        LedgerEntriesCursor cursor = new LedgerEntriesCursor(copenhagen, Guid.CreateVersion7());

        LedgerEntriesCursor.TryDecode(cursor.Encode(), out LedgerEntriesCursor? result);

        Assert.NotNull(result);
        Assert.Equal(copenhagen.UtcDateTime, result.CreatedAt.UtcDateTime);
        Assert.Equal(TimeSpan.Zero, result.CreatedAt.Offset);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not base64url!")]
    public void TryDecode_WithMissingOrMalformedValue_ReturnsFalse(string? value)
    {
        bool decoded = LedgerEntriesCursor.TryDecode(value, out LedgerEntriesCursor? cursor);

        Assert.False(decoded);
        Assert.Null(cursor);
    }

    // Valid base64url, but not "ticks|guid" inside: a client edited the cursor or sent one from somewhere else.
    [Theory]
    [InlineData("hello")]
    [InlineData("123")]
    [InlineData("123|not-a-guid")]
    [InlineData("-5|0193a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b")]
    [InlineData("99999999999999999999|0193a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b")]
    [InlineData("1|2|0193a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b")]
    public void TryDecode_WithWellFormedButWrongContent_ReturnsFalse(string raw)
    {
        string value = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(raw));

        bool decoded = LedgerEntriesCursor.TryDecode(value, out LedgerEntriesCursor? cursor);

        Assert.False(decoded);
        Assert.Null(cursor);
    }
}