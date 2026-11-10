using Microsoft.Extensions.Logging;
using PulseWallet.Application.Abstractions;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.Wallets.CreateWallet;

public class CreateWalletHandler
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWalletQueries _walletQueries;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CreateWalletHandler> _logger;

    public CreateWalletHandler(
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        IWalletQueries walletQueries,
        TimeProvider timeProvider,
        ILogger<CreateWalletHandler> logger)
    {
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _walletQueries = walletQueries;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<WalletDetails> HandleAsync(CreateWalletCommand command, CancellationToken cancellationToken)
    {
        Wallet wallet = Wallet.Create(command.Username, command.Currency, _timeProvider.GetUtcNow());

        if (await _walletRepository.UsernameExistsAsync(wallet.Username, cancellationToken))
        {
            throw new UsernameAlreadyTakenException(wallet.Username);
        }

        _walletRepository.Add(wallet);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet {WalletId} created for {Username}", wallet.Id, wallet.Username);

        // Read back through the read side, so POST and GET return the same shape built the same way.
        return await _walletQueries.GetDetailsAsync(wallet.Id, WalletDetails.RecentEntriesCount, cancellationToken)
            ?? throw new InvalidOperationException($"Wallet '{wallet.Id}' was not found right after it was saved.");
    }
}