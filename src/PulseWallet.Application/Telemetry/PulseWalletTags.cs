namespace PulseWallet.Application.Telemetry;

// Span tag names in one place: a backend query like "pulsewallet.idempotent_replay = true" only works if every
// handler spells the tag the same way. Prefixed so they never clash with OpenTelemetry's own names (http.*, db.*).
// Ids are fine as span tags (each span is one event); they must never become metric tags, where every distinct
// value creates a new time series.
public static class PulseWalletTags
{
    public const string WalletId = "pulsewallet.wallet.id";
    public const string TransferId = "pulsewallet.transfer.id";
    public const string IdempotentReplay = "pulsewallet.idempotent_replay";
    public const string CrossCurrency = "pulsewallet.transfer.cross_currency";
}