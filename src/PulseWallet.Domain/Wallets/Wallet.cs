using PulseWallet.Domain.Common;
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
}