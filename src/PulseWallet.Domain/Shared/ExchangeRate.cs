using PulseWallet.Domain.Common;

namespace PulseWallet.Domain.Shared;

public sealed record ExchangeRate
{
    public const int MaxDecimals = 8;

    public Currency From { get; }
    public Currency To { get; }
    public decimal Rate { get; }

    // The day the rate was published by its source (the ECB publishes once per working day),
    // not the moment we fetched it: on a Monday morning a freshly fetched rate is Friday's.
    public DateOnly Date { get; }

    private ExchangeRate(Currency from, Currency to, decimal rate, DateOnly date)
    {
        From = from;
        To = to;
        Rate = rate;
        Date = date;
    }

    public static ExchangeRate Create(Currency from, Currency to, decimal rate, DateOnly date)
    {
        if (!Enum.IsDefined(from) || !Enum.IsDefined(to))
        {
            throw new DomainException($"Currency pair '{from}/{to}' is not supported.");
        }

        if (from == to)
        {
            throw new DomainException("An exchange rate needs two different currencies.");
        }

        if (rate <= 0)
        {
            throw new DomainException("An exchange rate must be greater than zero.");
        }

        if (decimal.Round(rate, MaxDecimals) != rate)
        {
            throw new DomainException($"Exchange rate '{rate}' has more than {MaxDecimals} decimal places.");
        }

        return new ExchangeRate(from, to, rate, date);
    }
}