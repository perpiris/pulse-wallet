using Microsoft.Extensions.Logging;
using PulseWallet.Application.Abstractions;
using PulseWallet.Application.Ledger;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.Wallets.WithdrawFromWallet;

public sealed class WithdrawFromWalletHandler
{
    private readonly ILedgerEntryRepository _ledgerEntryRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWalletQueries _walletQueries;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WithdrawFromWalletHandler> _logger;

    public WithdrawFromWalletHandler(
        ILedgerEntryRepository ledgerEntryRepository,
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        IWalletQueries walletQueries,
        TimeProvider timeProvider,
        ILogger<WithdrawFromWalletHandler> logger)
    {
        _ledgerEntryRepository = ledgerEntryRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _walletQueries = walletQueries;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<WalletDetails> HandleAsync(WithdrawFromWalletCommand command, CancellationToken cancellationToken)
    {
        Money amount = Money.Create(command.Amount, command.Currency);

        // A retry of a request that already succeeded: don't move money again, return the current state.
        // Checked before the balance, so a replay never fails with "insufficient funds" because of its own first attempt.
        LedgerEntry? existing = await _ledgerEntryRepository.GetByIdempotencyKeyAsync(
            command.WalletId, command.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!existing.Matches(LedgerEntryType.Withdrawal, amount))
            {
                throw new IdempotencyKeyReusedException(command.IdempotencyKey);
            }

            _logger.LogInformation("Withdrawal with idempotency key {IdempotencyKey} already applied (entry {LedgerEntryId}), replaying",
                command.IdempotencyKey, existing.Id);
            return await GetDetailsAsync(command.WalletId, cancellationToken);
        }

        Wallet? wallet = await _walletRepository.GetByIdAsync(command.WalletId, cancellationToken);
        if (wallet is null)
        {
            throw new WalletNotFoundException(command.WalletId);
        }

        LedgerEntry entry = wallet.Withdraw(amount, command.IdempotencyKey, _timeProvider.GetUtcNow());
        _ledgerEntryRepository.Add(entry);

        // One SaveChanges: the new balance and the ledger entry (with its key) are saved in the same transaction.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet {WalletId} withdrew {Amount} {Currency} (entry {LedgerEntryId})",
            wallet.Id, command.Amount, command.Currency, entry.Id);

        return await GetDetailsAsync(wallet.Id, cancellationToken);
    }

    private async Task<WalletDetails> GetDetailsAsync(Guid walletId, CancellationToken cancellationToken)
    {
        return await _walletQueries.GetDetailsAsync(walletId, WalletDetails.RecentEntriesCount, cancellationToken)
               ?? throw new WalletNotFoundException(walletId);
    }
}