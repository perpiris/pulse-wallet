using PulseWallet.Domain.Ledger;

namespace PulseWallet.Domain.Transfers;

public sealed record TransferExecution(Transfer Transfer, LedgerEntry Debit, LedgerEntry Credit);