using PulseWallet.Domain.Common;

namespace PulseWallet.Domain.Wallets;

/// <summary>
/// A valid withdrawal that the current balance doesn't allow. It is a DomainException (a broken business rule),
/// but gets its own response (422), so GlobalExceptionHandler must match it before DomainException.
/// </summary>
public class InsufficientFundsException(Guid walletId, decimal requested, decimal available)
    : DomainException($"Wallet '{walletId}' has insufficient funds: requested {requested}, available {available}.");