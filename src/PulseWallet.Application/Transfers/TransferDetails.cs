using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.Transfers;

public record TransferDetails(
    Guid Id,
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    Currency Currency,
    decimal ReceivedAmount,
    Currency ReceivedCurrency,
    decimal? Rate,
    DateOnly? RateDate,
    DateTimeOffset CreatedAt);