namespace PulseWallet.Domain.Ledger;

public interface ILedgerEntryRepository
{
    void Add(LedgerEntry entry);
}