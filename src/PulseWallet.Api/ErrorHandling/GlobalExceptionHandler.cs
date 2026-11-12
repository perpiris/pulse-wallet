using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PulseWallet.Application.Ledger;
using PulseWallet.Application.Transfers;
using PulseWallet.Application.Wallets;
using PulseWallet.Application.Wallets.CreateWallet;
using PulseWallet.Domain.Common;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Api.ErrorHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    // SQL Server error numbers for a duplicate key in a unique index / unique constraint.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    // Chosen as the deadlock victim: two transactions locked the same rows in opposite order
    // (for example A→B and B→A transfers at the same moment). Nothing was saved, a retry is safe.
    private const int DeadlockVictim = 1205;

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The first matching arm wins: specific DomainException subtypes must come before DomainException.
        (int statusCode, string title) = exception switch
        {
            InsufficientFundsException => (StatusCodes.Status422UnprocessableEntity, "Insufficient funds"),
            IdempotencyKeyReusedException => (StatusCodes.Status422UnprocessableEntity, "Idempotency key reused"),
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
            WalletNotFoundException => (StatusCodes.Status404NotFound, "Wallet not found"),
            TransferNotFoundException => (StatusCodes.Status404NotFound, "Transfer not found"),
            UsernameAlreadyTakenException => (StatusCodes.Status409Conflict, "Username already taken"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrent update"),
            DbUpdateException { InnerException: SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation or DeadlockVictim } }
                => (StatusCodes.Status409Conflict, "Concurrent update"),
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
                Detail = GetDetail(exception, statusCode)
            }
        });
    }

    private static string GetDetail(Exception exception, int statusCode)
    {
        // Both mean "another request changed the same data at the same moment": the row version didn't match,
        // or a unique index (idempotency key, username) was claimed first. A retry then sees the winner's result.
        if (exception is DbUpdateConcurrencyException || (statusCode == StatusCodes.Status409Conflict && exception is DbUpdateException))
        {
            // EF's own message talks about rows and columns; the client needs to know it can simply retry.
            return "The wallet was changed by another request at the same time. Reload it and try again.";
        }

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            return "An unexpected error occurred.";
        }

        return exception.Message;
    }
}