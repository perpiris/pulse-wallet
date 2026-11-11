using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace PulseWallet.Application.Ledger;

public sealed record LedgerEntriesCursor(DateTimeOffset CreatedAt, Guid Id)
{
    public string Encode()
    {
        string raw = $"{CreatedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}|{Id}";
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(raw));
    }

    public static bool TryDecode(string? value, out LedgerEntriesCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string raw;
        try
        {
            raw = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value));
        }
        catch (FormatException)
        {
            return false;
        }

        string[] parts = raw.Split('|');
        if (parts.Length != 2
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks)
            || ticks < DateTimeOffset.MinValue.UtcTicks || ticks > DateTimeOffset.MaxValue.UtcTicks
            || !Guid.TryParse(parts[1], out Guid id))
        {
            return false;
        }

        cursor = new LedgerEntriesCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id);
        return true;
    }
}