namespace PulseWallet.Infrastructure.ExchangeRates;

// Frankfurter's JSON shape stays in Infrastructure; the rest of the app only sees ExchangeRate.
// Example: {"amount":1.0,"base":"USD","date":"2026-11-18","rates":{"DKK":6.8147}}
public sealed record FrankfurterLatestResponse(DateOnly Date, IReadOnlyDictionary<string, decimal> Rates);