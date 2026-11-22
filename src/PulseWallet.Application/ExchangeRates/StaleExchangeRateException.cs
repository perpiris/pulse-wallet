using System.Globalization;
using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.ExchangeRates;

// Not a failure the sender can only retry: they can agree to the rate and send again with its date as
// AcceptedRateDate. The rate travels with the exception so the API can show it in the response.
public class StaleExchangeRateException(ExchangeRate rate, int maxAgeInDays)
    : Exception(
        $"The {rate.From}/{rate.To} exchange rate is from {rate.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}, " +
        $"more than {maxAgeInDays} days old. Confirm that you accept it to send this transfer.")
{
    public ExchangeRate Rate { get; } = rate;
}