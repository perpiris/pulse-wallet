using Microsoft.AspNetCore.Mvc;
using PulseWallet.Api.Contracts;
using PulseWallet.Application.Wallets.CreateWallet;

namespace PulseWallet.Api.Controllers;

[ApiController]
[Route("api/wallets")]
[Produces("application/json")]
public class WalletsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateWallet(
        CreateWalletRequest request,
        [FromServices] CreateWalletHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return Ok();
    }
}