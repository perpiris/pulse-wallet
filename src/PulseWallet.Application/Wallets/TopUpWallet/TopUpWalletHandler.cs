using Microsoft.Extensions.Logging;
using PulseWallet.Application.Abstractions;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.Wallets.TopUpWallet;

public sealed class TopUpWalletHandler
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
        Wallet? wallet = await _walletRepository.GetByIdAsync(command.WalletId, cancellationToken);
        if (wallet is null)
        {
            throw new WalletNotFoundException(command.WalletId);
        }

        LedgerEntry entry = wallet.TopUp(Money.Create(command.Amount, command.Currency), _timeProvider.GetUtcNow());
        _ledgerEntryRepository.Add(entry);

        // One SaveChanges: the new balance and the ledger entry are saved in the same transaction.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet {WalletId} topped up with {Amount} {Currency} (entry {LedgerEntryId})",
            wallet.Id, command.Amount, command.Currency, entry.Id);

        return await _walletQueries.GetDetailsAsync(wallet.Id, WalletDetails.RecentEntriesCount, cancellationToken)
               ?? throw new InvalidOperationException($"Wallet '{wallet.Id}' was not found right after it was saved.");
    }
}