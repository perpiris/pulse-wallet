namespace PulseWallet.Infrastructure.Http;

public class RetryHandler : DelegatingHandler
{
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly TimeProvider _timeProvider;

    public RetryHandler(int maxRetries, TimeSpan baseDelay, TimeProvider timeProvider)
    {
        _maxRetries = maxRetries;
        _baseDelay = baseDelay;
        _timeProvider = timeProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Only GET is retried: a POST that timed out may still have done its work on the other side
        // (charged a card, moved money), and sending it again would do it twice.
        if (request.Method != HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        for (int attempt = 0; ; attempt++)
        {
            bool isLastAttempt = attempt == _maxRetries;

            try
            {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                if (isLastAttempt || !TransientFailures.IsTransient(response.StatusCode))
                {
                    return response;
                }

                response.Dispose();
            }
            catch (Exception exception) when (!isLastAttempt && TransientFailures.IsTransient(exception))
            {
                // Swallowed on purpose: the loop tries again after the delay. The last attempt's exception is not caught.
            }

            await Task.Delay(GetDelay(attempt), _timeProvider, cancellationToken);
        }
    }

    // Exponential backoff (300ms, 600ms, 1.2s...) gives the provider time to recover.
    // Jitter (a random 50-150% of that) spreads clients out, so they don't all retry in the same millisecond.
    private TimeSpan GetDelay(int attempt)
    {
        double exponential = _baseDelay.TotalMilliseconds * Math.Pow(2, attempt);
        double jitter = 0.5 + Random.Shared.NextDouble();

        return TimeSpan.FromMilliseconds(exponential * jitter);
    }
}