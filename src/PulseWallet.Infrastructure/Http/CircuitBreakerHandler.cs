namespace PulseWallet.Infrastructure.Http;

public class CircuitBreakerHandler : DelegatingHandler
{
    private readonly CircuitBreaker _circuitBreaker;

    public CircuitBreakerHandler(CircuitBreaker circuitBreaker)
    {
        _circuitBreaker = circuitBreaker;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_circuitBreaker.TryEnter())
        {
            throw new CircuitBreakerOpenException(request.RequestUri?.Host);
        }

        try
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (TransientFailures.IsTransient(response.StatusCode))
            {
                _circuitBreaker.RecordFailure();
            }
            else
            {
                _circuitBreaker.RecordSuccess();
            }

            return response;
        }
        catch (Exception exception) when (TransientFailures.IsTransient(exception))
        {
            _circuitBreaker.RecordFailure();
            throw;
        }
        catch
        {
            _circuitBreaker.ReleaseTrial();
            throw;
        }
    }
}