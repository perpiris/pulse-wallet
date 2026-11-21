using System.Diagnostics;

namespace PulseWallet.Application.Telemetry;

// ActivitySource is part of the BCL (System.Diagnostics), so Application can create spans without depending on
// OpenTelemetry: the Api decides whether anything listens to this source and where the spans are exported.
// Static on purpose: one source for the whole process, as the .NET guidance recommends.
public static class PulseWalletTelemetry
{
    public const string SourceName = "PulseWallet";

    public static readonly ActivitySource ActivitySource = new(SourceName);
}