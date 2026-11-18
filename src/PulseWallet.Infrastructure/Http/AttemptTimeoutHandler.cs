namespace PulseWallet.Infrastructure.Http;

// Limits a single try. The limit for the whole call, retries included, is HttpClient.Timeout,
// which wraps every handler in the pipeline.
public class AttemptTimeoutHandler : DelegatingHandler
{
    private readonly TimeSpan _timeout;

    public AttemptTimeoutHandler(TimeSpan timeout)
    {
        _timeout = timeout;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_timeout);

        try
        {
            return await base.SendAsync(request, timeoutSource.Token);
        }
        // Cancelled, but not by the caller: our timer fired. A TimeoutException tells the retry and the breaker
        // "this try failed", while a real cancellation (the caller gave up) stays an OperationCanceledException.
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"No response within {_timeout.TotalSeconds}s.", exception);
        }
    }
}