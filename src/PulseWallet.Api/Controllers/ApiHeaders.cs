namespace PulseWallet.Api.Controllers;

public static class ApiHeaders
{
    // Required on money-moving requests: the client generates one per operation and reuses it on retries,
    // so a retry never moves money twice. Bind it with [BindRequired]: without it, a missing header
    // would silently bind to Guid.Empty. A malformed one is a 400 either way.
    public const string IdempotencyKey = "Idempotency-Key";
}