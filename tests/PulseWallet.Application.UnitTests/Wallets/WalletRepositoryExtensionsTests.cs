using PulseWallet.Application.Wallets;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.UnitTests.Wallets;

public sealed class WalletRepositoryExtensionsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();

    [Fact]
    public async Task GetOwnedAsync_ForTheOwner_ReturnsTheWallet()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Wallet wallet = Wallet.Create(OwnerId, "alice", Currency.EUR, Now);
        _walletRepository.GetByIdAsync(wallet.Id, cancellationToken).Returns(wallet);

        Wallet result = await _walletRepository.GetOwnedAsync(wallet.Id, OwnerId, cancellationToken);

        Assert.Same(wallet, result);
    }

    [Fact]
    public async Task GetOwnedAsync_ForAnotherUser_ThrowsNotFound()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Wallet wallet = Wallet.Create(OwnerId, "alice", Currency.EUR, Now);
        _walletRepository.GetByIdAsync(wallet.Id, cancellationToken).Returns(wallet);

        await Assert.ThrowsAsync<WalletNotFoundException>(
            () => _walletRepository.GetOwnedAsync(wallet.Id, Guid.CreateVersion7(), cancellationToken));
    }

    [Fact]
    public async Task GetOwnedAsync_ForMissingWallet_ThrowsNotFound()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid walletId = Guid.CreateVersion7();
        _walletRepository.GetByIdAsync(walletId, cancellationToken).Returns((Wallet?)null);

        await Assert.ThrowsAsync<WalletNotFoundException>(
            () => _walletRepository.GetOwnedAsync(walletId, OwnerId, cancellationToken));
    }
}
