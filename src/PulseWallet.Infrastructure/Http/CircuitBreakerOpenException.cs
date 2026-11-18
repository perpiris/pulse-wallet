namespace PulseWallet.Infrastructure.Http;

// Deliberately not an HttpRequestException: the retry handler must not retry it, since nothing was sent.
public class CircuitBreakerOpenException(string? host)
    : Exception($"Calls to '{host}' are paused after repeated failures.");