using Microsoft.Extensions.Logging;
using PulseWallet.Application.Abstractions;
using PulseWallet.Application.Ledger;
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
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CreateTransferHandler> _logger;

    public CreateTransferHandler(
        ITransferRepository transferRepository,
        IWalletRepository walletRepository,
        ILedgerEntryRepository ledgerEntryRepository,
        IUnitOfWork unitOfWork,
        ITransferQueries transferQueries,
        TimeProvider timeProvider,
        ILogger<CreateTransferHandler> logger)
    {
        _transferRepository = transferRepository;
        _walletRepository = walletRepository;
        _ledgerEntryRepository = ledgerEntryRepository;
        _unitOfWork = unitOfWork;
        _transferQueries = transferQueries;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TransferDetails> HandleAsync(CreateTransferCommand command, CancellationToken cancellationToken)
    {
        Money amount = Money.Create(command.Amount, command.Currency);

        // A retry of a transfer that already went through: don't send again, return the original transfer.
        Transfer? existing = await _transferRepository.GetByIdempotencyKeyAsync(
            command.SourceWalletId, command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!existing.Matches(command.DestinationWalletId, amount))
            {
                throw new IdempotencyKeyReusedException(command.IdempotencyKey);
            }

            LogTransferReplayed(command.IdempotencyKey, existing.Id);
            return await GetDetailsAsync(existing.Id, cancellationToken);
        }

        Wallet source = await GetWalletAsync(command.SourceWalletId, cancellationToken);
        Wallet destination = await GetWalletAsync(command.DestinationWalletId, cancellationToken);

        TransferExecution execution = Transfer.Execute(
            source, destination, amount, command.IdempotencyKey, _timeProvider.GetUtcNow());

        _transferRepository.Add(execution.Transfer);
        _ledgerEntryRepository.Add(execution.Debit);
        _ledgerEntryRepository.Add(execution.Credit);

        // One SaveChanges, one transaction: both balances, both entries and the transfer, or nothing.
        // A concurrent change to either wallet fails the row version check → 409, the client retries.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogTransferCreated(execution.Transfer.Id, command.Amount, command.Currency, source.Id, destination.Id);

        return await GetDetailsAsync(execution.Transfer.Id, cancellationToken);
    }

    private async Task<Wallet> GetWalletAsync(Guid walletId, CancellationToken cancellationToken)
    {
        return await _walletRepository.GetByIdAsync(walletId, cancellationToken)
               ?? throw new WalletNotFoundException(walletId);
    }

    private async Task<TransferDetails> GetDetailsAsync(Guid transferId, CancellationToken cancellationToken)
    {
        return await _transferQueries.GetDetailsAsync(transferId, cancellationToken)
               ?? throw new TransferNotFoundException(transferId);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Transfer with idempotency key {IdempotencyKey} already applied ({TransferId}), replaying")]
    private partial void LogTransferReplayed(Guid idempotencyKey, Guid transferId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Transfer {TransferId}: {Amount} {Currency} from wallet {SourceWalletId} to wallet {DestinationWalletId}")]
    private partial void LogTransferCreated(Guid transferId, decimal amount, Currency currency, Guid sourceWalletId, Guid destinationWalletId);
}