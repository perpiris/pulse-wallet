using System.Net;

namespace PulseWallet.Infrastructure.Http;

// Answers that may succeed if we ask again a moment later. Other 4xx (400, 404...) will fail the same way every time.
internal static class TransientFailures
{
    public static bool IsTransient(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            || (int)statusCode >= 500;
    }

    public static bool IsTransient(Exception exception)
    {
        return exception is HttpRequestException or TimeoutException;
    }
}