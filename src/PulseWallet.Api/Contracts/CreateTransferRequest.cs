using PulseWallet.Application.Transfers.CreateTransfer;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record CreateTransferRequest
{
    public required Guid SourceWalletId { get; init; }
    public required Guid DestinationWalletId { get; init; }
    public required decimal Amount { get; init; }
    public required Currency Currency { get; init; }

    public CreateTransferCommand ToCommand(Guid userId, Guid idempotencyKey)
    {
        return new CreateTransferCommand(userId, SourceWalletId, DestinationWalletId, Amount, Currency, idempotencyKey);
    }
}