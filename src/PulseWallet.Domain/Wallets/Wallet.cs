using PulseWallet.Domain.Common;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Domain.Wallets;

public sealed class Wallet
{
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 20;

    public Guid Id { get; private set; }
    public string Username { get; private set; } = null!;
    public Currency Currency { get; private set; }
    public decimal Balance { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Wallet()
    {
    }

    public static Wallet Create(string username, Currency currency, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("Username cannot be null or empty");
        }

        string normalizedUsername = username.Trim().ToLowerInvariant();
        if (!normalizedUsername.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_'))
        {
            throw new DomainException("Username cannot contain special characters");
        }

        if (normalizedUsername.Length is < UsernameMinLength or > UsernameMaxLength)
        {
            throw new DomainException(
                $"Username must be between {UsernameMinLength} and {UsernameMaxLength} characters long.");
        }

        return new Wallet
        {
            Id = Guid.CreateVersion7(),
            Username = normalizedUsername,
            Currency = currency,
            Balance = 0,
            CreatedAt = now
        };
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

    // Internal: only Transfer.Execute calls these, so a transfer can't be half done from outside the domain.
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