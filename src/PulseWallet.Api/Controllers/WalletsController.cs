using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PulseWallet.Api.Contracts;
using PulseWallet.Api.Extensions;
using PulseWallet.Application.Ledger;
using PulseWallet.Application.Wallets;
using PulseWallet.Application.Wallets.CreateWallet;
using PulseWallet.Application.Wallets.GetWalletById;
using PulseWallet.Application.Wallets.GetWalletLedgerEntries;
using PulseWallet.Application.Wallets.ListMyWallets;
using PulseWallet.Application.Wallets.TopUpWallet;
using PulseWallet.Application.Wallets.WithdrawFromWallet;

namespace PulseWallet.Api.Controllers;

[ApiController]
[Route("api/wallets")]
[Produces("application/json")]
// Every endpoint here requires a signed-in user (the fallback policy); someone else's wallet or transfer is a 404.
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class WalletsController : ControllerBase
{
    // The signed-in user's own wallets. No paging: a person has a handful of wallets, not thousands.
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WalletSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyWallets(
        [FromServices] ListMyWalletsHandler handler,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<WalletSummary> wallets = await handler.HandleAsync(User.GetUserId(), cancellationToken);
        return Ok(wallets.Select(WalletSummaryResponse.From).ToList());
    }

    // The error responses are produced by GlobalExceptionHandler, not here;
    // the attributes only tell the OpenAPI document (and Swagger) that they can happen.
    [HttpPost]
    [ProducesResponseType<WalletDetailsResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateWallet(
        CreateWalletRequest request,
        [FromServices] CreateWalletHandler handler,
        CancellationToken cancellationToken)
    {
        WalletDetails wallet = await handler.HandleAsync(request.ToCommand(User.GetUserId()), cancellationToken);
        return CreatedAtAction(nameof(GetWalletById), new { id = wallet.Id }, WalletDetailsResponse.From(wallet));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<WalletDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetWalletById(
        Guid id,
        [FromServices] GetWalletByIdHandler handler,
        CancellationToken cancellationToken)
    {
        WalletDetails wallet = await handler.HandleAsync(id, User.GetUserId(), cancellationToken);
        return Ok(WalletDetailsResponse.From(wallet));
    }

    [HttpGet("{id:guid}/ledger-entries")]
    [ProducesResponseType<LedgerEntriesPageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetWalletLedgerEntries(
        Guid id,
        [FromQuery] GetWalletLedgerEntriesRequest request,
        [FromServices] GetWalletLedgerEntriesHandler handler,
        CancellationToken cancellationToken)
    {
        LedgerEntriesPage page = await handler.HandleAsync(request.ToQuery(id, User.GetUserId()), cancellationToken);
        return Ok(LedgerEntriesPageResponse.From(page));
    }

    [HttpPost("{id:guid}/top-ups")]
    [ProducesResponseType<WalletDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> TopUpWallet(
        Guid id,
        TopUpWalletRequest request,
        [FromHeader(Name = ApiHeaders.IdempotencyKey), BindRequired] Guid idempotencyKey,
        [FromServices] TopUpWalletHandler handler,
        CancellationToken cancellationToken)
    {
        WalletDetails wallet = await handler.HandleAsync(request.ToCommand(id, User.GetUserId(), idempotencyKey), cancellationToken);
        return Ok(WalletDetailsResponse.From(wallet));
    }

    [HttpPost("{id:guid}/withdrawals")]
    [ProducesResponseType<WalletDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> WithdrawFromWallet(
        Guid id,
        WithdrawFromWalletRequest request,
        [FromHeader(Name = ApiHeaders.IdempotencyKey), BindRequired] Guid idempotencyKey,
        [FromServices] WithdrawFromWalletHandler handler,
        CancellationToken cancellationToken)
    {
        WalletDetails wallet = await handler.HandleAsync(request.ToCommand(id, User.GetUserId(), idempotencyKey), cancellationToken);
        return Ok(WalletDetailsResponse.From(wallet));
    }
}