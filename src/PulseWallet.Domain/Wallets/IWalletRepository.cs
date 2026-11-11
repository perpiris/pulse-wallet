namespace PulseWallet.Domain.Wallets;

public interface IWalletRepository
{
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken);
    Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(Wallet wallet);
}