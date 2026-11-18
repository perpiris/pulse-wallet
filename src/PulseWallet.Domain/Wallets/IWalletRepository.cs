namespace PulseWallet.Domain.Wallets;

public interface IWalletRepository
{
    Task<bool> FriendlyNameExistsAsync(Guid ownerId, string friendlyName, CancellationToken cancellationToken);
    Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(Wallet wallet);
}