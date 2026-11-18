using Microsoft.Extensions.Logging.Abstractions;
using PulseWallet.Application.Abstractions;
using PulseWallet.Application.Wallets;
using PulseWallet.Application.Wallets.CreateWallet;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Application.UnitTests.Wallets.CreateWallet;

public sealed class CreateWalletHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.CreateVersion7();

    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IWalletQueries _walletQueries = Substitute.For<IWalletQueries>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly CreateWalletHandler _handler;

    // xUnit creates a new instance of the class for every test, so each test gets fresh substitutes.
    public CreateWalletHandlerTests()
    {
        _timeProvider.GetUtcNow().Returns(Now);

        _handler = new CreateWalletHandler(
            _walletRepository,
            _unitOfWork,
            _walletQueries,
            _timeProvider,
            NullLogger<CreateWalletHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_WithFreeUsername_SavesWalletAndReturnsItsDetails()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        CreateWalletCommand command = new CreateWalletCommand(OwnerId, "Alice", Currency.EUR);

        _walletRepository.UsernameExistsAsync("alice", cancellationToken).Returns(false);

        Wallet? added = null;
        _walletRepository.Add(Arg.Do<Wallet>(wallet => added = wallet));

        // The read side is a substitute too: it answers with whatever id the handler asks for,
        // so the assertion on result.Id checks that the handler read back the wallet it just saved.
        _walletQueries
            .GetDetailsAsync(Arg.Any<Guid>(), OwnerId, WalletDetails.RecentEntriesCount, cancellationToken)
            .Returns(call => new WalletDetails(call.ArgAt<Guid>(0), "alice", Currency.EUR, 0m, Now, []));

        WalletDetails result = await _handler.HandleAsync(command, cancellationToken);

        Assert.NotNull(added);
        Assert.Equal("alice", added.Username);
        Assert.Equal(OwnerId, added.OwnerId);
        Assert.Equal(Now, added.CreatedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(cancellationToken);
        Assert.Equal(added.Id, result.Id);
    }
}