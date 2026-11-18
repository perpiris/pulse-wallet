using Microsoft.Extensions.Logging;
using PulseWallet.Application.Abstractions;
using PulseWallet.Application.Ledger;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.Wallets.TopUpWallet;

public sealed partial class TopUpWalletHandler
{
    private readonly ILedgerEntryRepository _ledgerEntryRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWalletQueries _walletQueries;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TopUpWalletHandler> _logger;

    public TopUpWalletHandler(
        ILedgerEntryRepository ledgerEntryRepository,
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        IWalletQueries walletQueries,
        TimeProvider timeProvider,
        ILogger<TopUpWalletHandler> logger)
    {
        _ledgerEntryRepository = ledgerEntryRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _walletQueries = walletQueries;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<WalletDetails> HandleAsync(TopUpWalletCommand command, CancellationToken cancellationToken)
    {
        Money amount = Money.Create(command.Amount, command.Currency);

        // A retry of a request that already succeeded: don't move money again, return the current state.
        LedgerEntry? existing = await _ledgerEntryRepository.GetByIdempotencyKeyAsync(
            command.WalletId, command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!existing.Matches(LedgerEntryType.TopUp, amount))
            {
                throw new IdempotencyKeyReusedException(command.IdempotencyKey);
            }

            LogTopUpReplayed(command.IdempotencyKey, existing.Id);
            return await GetDetailsAsync(command.WalletId, cancellationToken);
        }

        Wallet? wallet = await _walletRepository.GetByIdAsync(command.WalletId, cancellationToken);
        if (wallet is null)
        {
            throw new WalletNotFoundException(command.WalletId);
        }

        LedgerEntry entry = wallet.TopUp(amount, command.IdempotencyKey, _timeProvider.GetUtcNow());
        _ledgerEntryRepository.Add(entry);

        // One SaveChanges: the new balance and the ledger entry (with its key) are saved in the same transaction.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogToppedUp(wallet.Id, command.Amount, command.Currency, entry.Id);

        return await GetDetailsAsync(wallet.Id, cancellationToken);
    }

    private async Task<WalletDetails> GetDetailsAsync(Guid walletId, CancellationToken cancellationToken)
    {
        return await _walletQueries.GetDetailsAsync(walletId, WalletDetails.RecentEntriesCount, cancellationToken)
               ?? throw new WalletNotFoundException(walletId);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Top-up with idempotency key {IdempotencyKey} already applied (entry {LedgerEntryId}), replaying")]
    private partial void LogTopUpReplayed(Guid idempotencyKey, Guid ledgerEntryId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Wallet {WalletId} topped up with {Amount} {Currency} (entry {LedgerEntryId})")]
    private partial void LogToppedUp(Guid walletId, decimal amount, Currency currency, Guid ledgerEntryId);
}