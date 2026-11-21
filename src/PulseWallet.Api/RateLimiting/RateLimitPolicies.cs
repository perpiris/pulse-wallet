namespace PulseWallet.Api.RateLimiting;

public static class RateLimitPolicies
{
    // Top-ups, withdrawals and transfers: on top of the global limit, which every request already counts towards.
    public const string MoneyMovement = "money-movement";
}