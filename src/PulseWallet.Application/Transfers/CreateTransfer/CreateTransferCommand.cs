using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Transfers.CreateTransfer;

public record CreateTransferCommand(
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    Currency Currency,
    Guid IdempotencyKey);