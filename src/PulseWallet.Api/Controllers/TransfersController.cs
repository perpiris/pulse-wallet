using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PulseWallet.Api.Contracts;
using PulseWallet.Api.Extensions;
using PulseWallet.Application.Transfers;
using PulseWallet.Application.Transfers.CreateTransfer;
using PulseWallet.Application.Transfers.GetTransferById;

namespace PulseWallet.Api.Controllers;

[ApiController]
[Route("api/transfers")]
[Produces("application/json")]
// Every endpoint here requires a signed-in user (the fallback policy); someone else's wallet or transfer is a 404.
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class TransfersController : ControllerBase
{
    // A replay (same Idempotency-Key, same body) returns the original transfer with 201 again,
    // so the client sees the same response it would have got the first time.
    [HttpPost]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> CreateTransfer(
        CreateTransferRequest request,
        [FromHeader(Name = ApiHeaders.IdempotencyKey), BindRequired] Guid idempotencyKey,
        [FromServices] CreateTransferHandler handler,
        CancellationToken cancellationToken)
    {
        TransferDetails transfer = await handler.HandleAsync(request.ToCommand(User.GetUserId(), idempotencyKey), cancellationToken);
        return CreatedAtAction(nameof(GetTransferById), new { id = transfer.Id }, TransferResponse.From(transfer));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetTransferById(
        Guid id,
        [FromServices] GetTransferByIdHandler handler,
        CancellationToken cancellationToken)
    {
        TransferDetails transfer = await handler.HandleAsync(id, User.GetUserId(), cancellationToken);
        return Ok(TransferResponse.From(transfer));
    }
}