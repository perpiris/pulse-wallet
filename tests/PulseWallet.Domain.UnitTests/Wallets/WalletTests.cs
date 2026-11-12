using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Domain.UnitTests.Wallets;

public sealed class WalletTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidUsername_ReturnsEmptyWalletWithNormalizedUsername()
    {
        Wallet wallet = Wallet.Create("  Alice_01 ", Currency.EUR, Now);

        Assert.NotEqual(Guid.Empty, wallet.Id);
        Assert.Equal("alice_01", wallet.Username);
        Assert.Equal(Currency.EUR, wallet.Currency);
        Assert.Equal(0m, wallet.Balance);
        Assert.Equal(Now, wallet.CreatedAt);
    }
}