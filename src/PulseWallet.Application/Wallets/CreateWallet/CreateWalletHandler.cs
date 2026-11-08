using Microsoft.Extensions.Logging;

namespace PulseWallet.Application.Wallets.CreateWallet;

public class CreateWalletHandler
{
    private readonly ILogger<CreateWalletHandler> _logger;

    public CreateWalletHandler(ILogger<CreateWalletHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(CreateWalletCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Wallet {WalletId} created for user {Username}", Guid.CreateVersion7().ToString(), command.Username);
        return Task.CompletedTask;
    }
}