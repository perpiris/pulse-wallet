namespace PulseWallet.Application.Ledger;

public class IdempotencyKeyReusedException(Guid idempotencyKey)
    : Exception($"Idempotency key '{idempotencyKey}' was already used for a different request.");