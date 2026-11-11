using Microsoft.AspNetCore.Mvc;
using PulseWallet.Api.Contracts;
using PulseWallet.Application.Wallets;
using PulseWallet.Application.Wallets.CreateWallet;
using PulseWallet.Application.Wallets.GetWalletById;
using PulseWallet.Application.Wallets.TopUpWallet;
using PulseWallet.Application.Wallets.WithdrawFromWallet;

namespace PulseWallet.Api.Controllers;

[ApiController]
[Route("api/wallets")]
[Produces("application/json")]
public class WalletsController : ControllerBase
{
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
        WalletDetails wallet = await handler.HandleAsync(request.ToCommand(), cancellationToken);
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
        WalletDetails wallet = await handler.HandleAsync(id, cancellationToken);
        return Ok(WalletDetailsResponse.From(wallet));
    }

    [HttpPost("{id:guid}/top-ups")]
    [ProducesResponseType<WalletDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> TopUpWallet(
        Guid id,
        TopUpWalletRequest request,
        [FromServices] TopUpWalletHandler handler,
        CancellationToken cancellationToken)
    {
        WalletDetails wallet = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
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
        [FromServices] WithdrawFromWalletHandler handler,
        CancellationToken cancellationToken)
    {
        WalletDetails wallet = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return Ok(WalletDetailsResponse.From(wallet));
    }
}