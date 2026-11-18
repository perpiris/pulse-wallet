using Microsoft.Extensions.Logging;
using PulseWallet.Application.Abstractions;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.Wallets.CreateWallet;

public partial class CreateWalletHandler
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
        Wallet wallet = Wallet.Create(command.OwnerId, command.FriendlyName, command.Currency, _timeProvider.GetUtcNow());

        // A friendly check for the common case; the unique (OwnerId, FriendlyName) index is the real guarantee
        // when two requests race (that one surfaces as a 409 from the exception handler).
        if (await _walletRepository.FriendlyNameExistsAsync(wallet.OwnerId, wallet.FriendlyName, cancellationToken))
        {
            throw new FriendlyNameAlreadyUsedException(wallet.FriendlyName);
        }

        _walletRepository.Add(wallet);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogWalletCreated(wallet.Id, wallet.OwnerId);

        // Read back through the read side, so POST and GET return the same shape built the same way.
        return await _walletQueries.GetDetailsAsync(
                   wallet.Id, command.OwnerId, WalletDetails.RecentEntriesCount, cancellationToken)
            ?? throw new InvalidOperationException($"Wallet '{wallet.Id}' was not found right after it was saved.");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Wallet {WalletId} created for owner {OwnerId}")]
    private partial void LogWalletCreated(Guid walletId, Guid ownerId);
}