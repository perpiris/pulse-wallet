using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.ExchangeRates;

public class ExchangeRateUnavailableException(Currency from, Currency to, Exception? innerException = null)
    : Exception($"The {from}/{to} exchange rate is unavailable right now. Try again later.", innerException);