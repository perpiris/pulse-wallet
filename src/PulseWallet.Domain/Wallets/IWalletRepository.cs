namespace PulseWallet.Domain.Wallets;

public interface IWalletRepository
{
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Wallet wallet);
}