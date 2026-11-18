using PulseWallet.Domain.Shared;

namespace PulseWallet.Application.ExchangeRates;

public interface IExchangeRateProvider
{
    /// <exception cref="ExchangeRateUnavailableException">The rate source can't be reached or gave an unusable answer.</exception>
    Task<ExchangeRate> GetLatestRateAsync(Currency from, Currency to, CancellationToken cancellationToken);
}