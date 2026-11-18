using PulseWallet.Application.Transfers;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Api.Contracts;

public record TransferResponse(
    Guid Id,
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    Currency Currency,
    decimal ReceivedAmount,
    Currency ReceivedCurrency,
    decimal? Rate,
    DateOnly? RateDate,
    DateTimeOffset CreatedAt)
{
    public static TransferResponse From(TransferDetails details)
    {
        return new TransferResponse(
            details.Id,
            details.SourceWalletId,
            details.DestinationWalletId,
            details.Amount,
            details.Currency,
            details.ReceivedAmount,
            details.ReceivedCurrency,
            details.Rate,
            details.RateDate,
            details.CreatedAt);
    }
}