using Microsoft.AspNetCore.Identity;

namespace PulseWallet.Infrastructure.Identity;

// The sign-in account, not a domain concept: it lives in Infrastructure so the domain doesn't depend on
// ASP.NET Core Identity. Wallets will refer to it only by its Id (a Guid, like every other id here).
public class ApplicationUser : IdentityUser<Guid>
{
}