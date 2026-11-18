using System.Security.Claims;

namespace PulseWallet.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // The Identity user's id, from the NameIdentifier claim in the bearer token.
    // Only called behind the fallback policy, so a signed-in user is guaranteed; a missing or malformed claim
    // means a broken token setup, which should fail loudly (500) rather than act as some other user.
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        string? value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out Guid userId))
        {
            throw new InvalidOperationException("The signed-in user has no valid NameIdentifier claim.");
        }

        return userId;
    }
}