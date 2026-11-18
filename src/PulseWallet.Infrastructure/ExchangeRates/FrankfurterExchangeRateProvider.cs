using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PulseWallet.Application.ExchangeRates;
using PulseWallet.Domain.Shared;
using PulseWallet.Infrastructure.Http;

namespace PulseWallet.Infrastructure.ExchangeRates;

// A typed client: IHttpClientFactory creates the HttpClient with the base address and the retry, circuit breaker
// and timeout handlers from DependencyInjection, so this class has no retry or timeout code of its own.
public partial class FrankfurterExchangeRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterExchangeRateProvider> _logger;

    public FrankfurterExchangeRateProvider(HttpClient httpClient, ILogger<FrankfurterExchangeRateProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExchangeRate> GetLatestRateAsync(Currency from, Currency to, CancellationToken cancellationToken)
    {
        Uri requestUri = new Uri($"latest?base={from}&symbols={to}", UriKind.Relative);
        FrankfurterLatestResponse? response;

        try
        {
            response = await _httpClient.GetFromJsonAsync<FrankfurterLatestResponse>(requestUri, cancellationToken);
        }
        // Reached only after the handlers gave up:
        // HttpRequestException = connection error or non-success status on the last try,
        // TimeoutException = the last try took longer than the attempt timeout,
        // CircuitBreakerOpenException = the circuit is open, nothing was sent,
        // OperationCanceledException not caused by the caller = HttpClient.Timeout (the total) ran out,
        // JsonException = the provider answered with something we can't read.
        // HTTP and handler types must not leak into Application, so they all become one exception of ours.
        catch (Exception exception) when (
            exception is HttpRequestException or TimeoutException or CircuitBreakerOpenException or JsonException
            || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            LogRateUnavailable(exception, from, to);
            throw new ExchangeRateUnavailableException(from, to, exception);
        }

        if (response?.Rates is null || !response.Rates.TryGetValue(to.ToString(), out decimal rate) || rate <= 0)
        {
            LogUnexpectedResponse(from, to);
            throw new ExchangeRateUnavailableException(from, to);
        }

        // ECB rates have at most 5 decimals; the rounding only matters if the source ever sends more than we store.
        return ExchangeRate.Create(from, to, decimal.Round(rate, ExchangeRate.MaxDecimals), response.Date);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exchange rate {From}/{To} unavailable from Frankfurter")]
    private partial void LogRateUnavailable(Exception exception, Currency from, Currency to);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Frankfurter returned no usable {From}/{To} rate")]
    private partial void LogUnexpectedResponse(Currency from, Currency to);
}