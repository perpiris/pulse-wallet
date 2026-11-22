using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PulseWallet.Application.Abstractions;
using PulseWallet.Application.ExchangeRates;
using PulseWallet.Application.Ledger;
using PulseWallet.Application.Telemetry;
using PulseWallet.Application.Wallets;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Transfers;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.Transfers.CreateTransfer;

public sealed partial class CreateTransferHandler
{
    private readonly ITransferRepository _transferRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ILedgerEntryRepository _ledgerEntryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransferQueries _transferQueries;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly ExchangeRateAgePolicy _exchangeRateAgePolicy;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CreateTransferHandler> _logger;

    public CreateTransferHandler(
        ITransferRepository transferRepository,
        IWalletRepository walletRepository,
        ILedgerEntryRepository ledgerEntryRepository,
        IUnitOfWork unitOfWork,
        ITransferQueries transferQueries,
        IExchangeRateProvider exchangeRateProvider,
        ExchangeRateAgePolicy exchangeRateAgePolicy,
        TimeProvider timeProvider,
        ILogger<CreateTransferHandler> logger)
    {
        _transferRepository = transferRepository;
        _walletRepository = walletRepository;
        _ledgerEntryRepository = ledgerEntryRepository;
        _unitOfWork = unitOfWork;
        _transferQueries = transferQueries;
        _exchangeRateProvider = exchangeRateProvider;
        _exchangeRateAgePolicy = exchangeRateAgePolicy;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TransferDetails> HandleAsync(CreateTransferCommand command, CancellationToken cancellationToken)
    {
        using Activity? activity = PulseWalletTelemetry.ActivitySource.StartActivity("CreateTransfer");
        activity?.SetTag(PulseWalletTags.WalletId, command.SourceWalletId);

        Money amount = Money.Create(command.Amount, command.Currency);

        // You can only send from your own wallet, but to anyone's. Checked before the replay, for the same
        // reason as in the top-up handler: a replay must not reveal a transfer to someone who doesn't own the source.
        Wallet source = await _walletRepository.GetOwnedAsync(command.SourceWalletId, command.UserId, cancellationToken);

        // A retry of a transfer that already went through: don't send again, return the original transfer.
        Transfer? existing = await _transferRepository.GetByIdempotencyKeyAsync(
            command.SourceWalletId, command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!existing.Matches(command.DestinationWalletId, amount))
            {
                throw new IdempotencyKeyReusedException(command.IdempotencyKey);
            }

            activity?.SetTag(PulseWalletTags.IdempotentReplay, true);
            activity?.SetTag(PulseWalletTags.TransferId, existing.Id);
            LogTransferReplayed(command.IdempotencyKey, existing.Id);
            return await GetDetailsAsync(existing.Id, command.UserId, cancellationToken);
        }

        Wallet destination = await GetWalletAsync(command.DestinationWalletId, cancellationToken);
        activity?.SetTag(PulseWalletTags.CrossCurrency, source.Currency != destination.Currency);

        // Fetched before anything is written: with retries the call can take seconds, and no transaction is open yet
        // (SaveChanges opens the only one), so nothing is locked meanwhile. If a wallet changes during the wait,
        // its row version catches it on save → 409. Provider down → 503, and same-currency transfers never get here.
        ExchangeRate? rate = source.Currency == destination.Currency
            ? null
            : await _exchangeRateProvider.GetLatestRateAsync(source.Currency, destination.Currency, cancellationToken);

        // A stale rate is only used once the sender has seen it: the first attempt gets a 422 carrying the rate,
        // and the client sends again with that rate's date. Matching the date, not just a "yes" flag, means the
        // agreement covers exactly the rate they saw; if a newer one arrived meanwhile, it's fresh and needs none.
        if (rate is not null && _exchangeRateAgePolicy.IsStale(rate) && command.AcceptedRateDate != rate.Date)
        {
            throw new StaleExchangeRateException(rate, _exchangeRateAgePolicy.MaxAgeInDays);
        }

        TransferExecution execution = Transfer.Execute(
            source, destination, amount, rate, command.IdempotencyKey, _timeProvider.GetUtcNow());

        _transferRepository.Add(execution.Transfer);
        _ledgerEntryRepository.Add(execution.Debit);
        _ledgerEntryRepository.Add(execution.Credit);

        // One SaveChanges, one transaction: both balances, both entries and the transfer, or nothing.
        // A concurrent change to either wallet fails the row version check → 409, the client retries.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Transfer transfer = execution.Transfer;
        activity?.SetTag(PulseWalletTags.TransferId, transfer.Id);
        LogTransferCreated(
            transfer.Id, transfer.Amount.Amount, transfer.Amount.Currency,
            transfer.ReceivedAmount.Amount, transfer.ReceivedAmount.Currency, transfer.Rate, source.Id, destination.Id);

        return await GetDetailsAsync(transfer.Id, command.UserId, cancellationToken);
    }

    private async Task<Wallet> GetWalletAsync(Guid walletId, CancellationToken cancellationToken)
    {
        return await _walletRepository.GetByIdAsync(walletId, cancellationToken)
               ?? throw new WalletNotFoundException(walletId);
    }

    private async Task<TransferDetails> GetDetailsAsync(Guid transferId, Guid userId, CancellationToken cancellationToken)
    {
        return await _transferQueries.GetDetailsAsync(transferId, userId, cancellationToken)
               ?? throw new TransferNotFoundException(transferId);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Transfer with idempotency key {IdempotencyKey} already applied ({TransferId}), replaying")]
    private partial void LogTransferReplayed(Guid idempotencyKey, Guid transferId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Transfer {TransferId}: {Amount} {Currency} from wallet {SourceWalletId}, {ReceivedAmount} {ReceivedCurrency} " +
                  "to wallet {DestinationWalletId} (rate {Rate})")]
    private partial void LogTransferCreated(
        Guid transferId,
        decimal amount,
        Currency currency,
        decimal receivedAmount,
        Currency receivedCurrency,
        decimal? rate,
        Guid sourceWalletId,
        Guid destinationWalletId);
}