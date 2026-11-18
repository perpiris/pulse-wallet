using PulseWallet.Domain.Common;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Domain.Wallets;

public sealed class Wallet
{
    public const int FriendlyNameMaxLength = 40;

    public Guid Id { get; private set; }

    // The id of the user who owns the wallet. Just an id: the domain doesn't know about sign-in accounts.
    public Guid OwnerId { get; private set; }

    // The owner's own label for the wallet ("Savings", "Holiday fund 2027"), kept as typed. Unique per owner,
    // not globally: that's enforced by the database (OwnerId, FriendlyName index), since it spans wallets.
    public string FriendlyName { get; private set; } = null!;
    public Currency Currency { get; private set; }
    public decimal Balance { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Wallet()
    {
    }

    public static Wallet Create(Guid ownerId, string friendlyName, Currency currency, DateTimeOffset now)
    {
        if (ownerId == Guid.Empty)
        {
            throw new DomainException("A wallet must have an owner.");
        }

        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            throw new DomainException("A wallet needs a friendly name.");
        }

        string trimmedName = friendlyName.Trim();
        if (trimmedName.Length > FriendlyNameMaxLength)
        {
            throw new DomainException($"A friendly name can be at most {FriendlyNameMaxLength} characters long.");
        }

        // Free text, but no control characters (newlines, tabs, ...): the name is shown in lists and on one line.
        if (trimmedName.Any(char.IsControl))
        {
            throw new DomainException("A friendly name cannot contain control characters.");
        }

        if (!Enum.IsDefined(currency))
        {
            throw new DomainException($"Currency '{currency}' is not supported.");
        }

        return new Wallet
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            FriendlyName = trimmedName,
            Currency = currency,
            Balance = 0,
            CreatedAt = now
        };
    }

    public bool IsOwnedBy(Guid userId)
    {
        return OwnerId == userId;
    }

    public LedgerEntry TopUp(Money amount, Guid idempotencyKey, DateTimeOffset now)
    {
        EnsureSameCurrency(amount);

        LedgerEntry entry = LedgerEntry.Create(Id, LedgerEntryType.TopUp, amount, idempotencyKey, now);
        Balance += amount.Amount;
        return entry;
    }

    public LedgerEntry Withdraw(Money amount, Guid idempotencyKey, DateTimeOffset now)
    {
        EnsureSameCurrency(amount);
        EnsureSufficientFunds(amount);

        LedgerEntry entry = LedgerEntry.Create(Id, LedgerEntryType.Withdrawal, amount, idempotencyKey, now);
        Balance -= amount.Amount;
        return entry;
    }

    internal LedgerEntry SendTransfer(Guid transferId, Money amount, DateTimeOffset now)
    {
        EnsureSameCurrency(amount);
        EnsureSufficientFunds(amount);

        LedgerEntry entry = LedgerEntry.CreateForTransfer(Id, LedgerEntryType.TransferOut, amount, transferId, now);
        Balance -= amount.Amount;
        return entry;
    }

    internal LedgerEntry ReceiveTransfer(Guid transferId, Money amount, DateTimeOffset now)
    {
        EnsureSameCurrency(amount);

        LedgerEntry entry = LedgerEntry.CreateForTransfer(Id, LedgerEntryType.TransferIn, amount, transferId, now);
        Balance += amount.Amount;
        return entry;
    }

    private void EnsureSufficientFunds(Money amount)
    {
        if (amount.Amount > Balance)
        {
            throw new InsufficientFundsException(Id, amount.Amount, Balance);
        }
    }

    private void EnsureSameCurrency(Money amount)
    {
        if (amount.Currency != Currency)
        {
            throw new DomainException($"Wallet currency is {Currency}, but the amount is in {amount.Currency}.");
        }
    }
}