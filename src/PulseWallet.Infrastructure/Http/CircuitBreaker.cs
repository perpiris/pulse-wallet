namespace PulseWallet.Infrastructure.Http;

// The breaker's memory. It must outlive every request and every handler: IHttpClientFactory recycles handlers
// every couple of minutes, so this is a singleton and each CircuitBreakerHandler only points to it.
//
// Closed: calls go through, consecutive failures are counted.
// Open: after FailureThreshold failures in a row, calls are refused until BreakDuration has passed.
// Half-open: after that, one trial call goes through. Success closes the circuit, failure opens it again.
public sealed class CircuitBreaker
{
    private readonly Lock _lock = new Lock();
    private readonly int _failureThreshold;
    private readonly TimeSpan _breakDuration;
    private readonly TimeProvider _timeProvider;

    private int _consecutiveFailures;
    private DateTimeOffset? _openUntil;
    private bool _trialInProgress;

    public CircuitBreaker(int failureThreshold, TimeSpan breakDuration, TimeProvider timeProvider)
    {
        _failureThreshold = failureThreshold;
        _breakDuration = breakDuration;
        _timeProvider = timeProvider;
    }

    public bool TryEnter()
    {
        lock (_lock)
        {
            if (_openUntil is null)
            {
                return true;
            }

            if (_timeProvider.GetUtcNow() < _openUntil || _trialInProgress)
            {
                return false;
            }

            _trialInProgress = true;
            return true;
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _consecutiveFailures = 0;
            _openUntil = null;
            _trialInProgress = false;
        }
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _consecutiveFailures++;
            _trialInProgress = false;

            // A failed trial (_openUntil already set) reopens straight away, without counting up to the threshold again.
            if (_openUntil is not null || _consecutiveFailures >= _failureThreshold)
            {
                _openUntil = _timeProvider.GetUtcNow() + _breakDuration;
            }
        }
    }

    // The call ended without telling us anything about the provider (the caller cancelled, or an unexpected error).
    // Without this, such a trial call would leave _trialInProgress set and the circuit would stay open forever.
    public void ReleaseTrial()
    {
        lock (_lock)
        {
            _trialInProgress = false;
        }
    }
}