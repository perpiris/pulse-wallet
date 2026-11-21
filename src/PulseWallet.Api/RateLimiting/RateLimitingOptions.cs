namespace PulseWallet.Api.RateLimiting;

// Bound from the "RateLimiting" section of appsettings, so limits can change per environment without a code change.
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public FixedWindowLimit Global { get; init; } = new();

    public TokenBucketLimit MoneyMovement { get; init; } = new();
}

public class FixedWindowLimit
{
    public int PermitLimit { get; init; }

    public TimeSpan Window { get; init; }
}

public class TokenBucketLimit
{
    // The burst: how many requests a client with a full bucket can make back to back.
    public int TokenLimit { get; init; }

    // The sustained rate: TokensPerPeriod come back every ReplenishmentPeriod, up to TokenLimit.
    public int TokensPerPeriod { get; init; }

    public TimeSpan ReplenishmentPeriod { get; init; }
}