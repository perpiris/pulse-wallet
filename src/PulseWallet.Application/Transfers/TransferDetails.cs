using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Transfers;

public record TransferDetails(
    Guid Id,
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    Currency Currency,
    DateTimeOffset CreatedAt);