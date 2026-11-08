using Microsoft.AspNetCore.Mvc;

namespace PulseWallet.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WalletController : ControllerBase
{
    private readonly ILogger<WalletController> _logger;

    public WalletController(ILogger<WalletController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult CreateWallet()
    {
        _logger.LogInformation("Wallet Created");
        return Ok("Wallet Created");
    }
}