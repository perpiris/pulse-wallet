namespace PulseWallet.Application.Ledger;

/// <summary>
/// The Idempotency-Key was already used for a different operation (another type or amount).
/// A real retry sends exactly the same request; anything else is a client bug, so it's refused (422).
/// </summary>
public class IdempotencyKeyReusedException(Guid idempotencyKey)
    : Exception($"Idempotency key '{idempotencyKey}' was already used for a different request.");
