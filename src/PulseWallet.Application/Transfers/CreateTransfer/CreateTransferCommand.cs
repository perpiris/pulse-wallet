using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Transfers.CreateTransfer;

public record CreateTransferCommand(
    Guid UserId,
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    Currency Currency,
    Guid IdempotencyKey,
    // The publish date of a stale rate the sender agreed to; null when they haven't seen a warning.
    DateOnly? AcceptedRateDate);