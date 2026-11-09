using Microsoft.AspNetCore.Diagnostics;
using PulseWallet.Application.Wallets.CreateWallet;
using PulseWallet.Application.Wallets.GetWalletById;
using PulseWallet.Domain.Common;

namespace PulseWallet.Api.ErrorHandling;

/// <summary>
/// Turns every exception that reaches the top of the pipeline into a ProblemDetails response (RFC 9457).
/// Plugged into the built-in exception handler middleware (UseExceptionHandler), so controllers never need try/catch.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
            WalletNotFoundException => (StatusCodes.Status404NotFound, "Wallet not found"),
            UsernameAlreadyTakenException => (StatusCodes.Status409Conflict, "Username already taken"),
            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        // Expected failures (4xx) are the client's problem and already show in the request log.
        // Only unexpected ones are logged with the full exception, and their details are never sent to the client.
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred."
                    : exception.Message
            }
        });
    }
}