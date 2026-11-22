using PulseWallet.Application.Transfers.CreateTransfer;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record CreateTransferRequest
{
    public required Guid SourceWalletId { get; init; }
    public required Guid DestinationWalletId { get; init; }
    public required decimal Amount { get; init; }
    public required Currency Currency { get; init; }

    // Only sent after a 422 "Exchange rate is stale": the date from that response, meaning "I accept this rate".
    public DateOnly? AcceptedRateDate { get; init; }

    public CreateTransferCommand ToCommand(Guid userId, Guid idempotencyKey)
    {
        return new CreateTransferCommand(userId, SourceWalletId, DestinationWalletId, Amount, Currency, idempotencyKey, AcceptedRateDate);
    }
}