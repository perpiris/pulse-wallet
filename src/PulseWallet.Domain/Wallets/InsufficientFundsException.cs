using PulseWallet.Domain.Common;

namespace PulseWallet.Domain.Wallets;

public class InsufficientFundsException(Guid walletId, decimal requested, decimal available)
    : DomainException($"Wallet '{walletId}' has insufficient funds: requested {requested}, available {available}.");