using PulseWallet.Domain.Common;

namespace PulseWallet.Domain.Shared;

public sealed record Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, Currency currency)
    {
        if (!Enum.IsDefined(currency))
        {
            throw new DomainException($"Currency '{currency}' is not supported.");
        }

        if (amount < 0)
        {
            throw new DomainException("Amount cannot be negative.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException($"Amount '{amount}' has more than 2 decimal places.");
        }

        return new Money(amount, currency);
    }
}