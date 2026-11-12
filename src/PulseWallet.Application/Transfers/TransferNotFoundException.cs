namespace PulseWallet.Application.Transfers;

public class TransferNotFoundException(Guid transferId)
    : Exception($"Transfer '{transferId}' was not found.");