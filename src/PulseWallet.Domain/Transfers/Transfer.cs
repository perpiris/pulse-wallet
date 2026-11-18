using PulseWallet.Domain.Common;
using PulseWallet.Domain.Ledger;
using PulseWallet.Domain.Shared;
using PulseWallet.Domain.Wallets;

namespace PulseWallet.Domain.Transfers;

public sealed class Transfer
{
    public Guid Id { get; private set; }
    public Guid SourceWalletId { get; private set; }
    public Guid DestinationWalletId { get; private set; }
    // What the sender was debited, in the source wallet's currency.
    public Money Amount { get; private set; } = null!;

    // What the receiver was credited, in the destination wallet's currency. Equal to Amount without conversion.
    public Money ReceivedAmount { get; private set; } = null!;

    // The rate used and the day it was published; both null when the wallets share a currency.
    // Stored so the conversion can be explained and audited later, whatever the rate is today.
    public decimal? Rate { get; private set; }
    public DateOnly? RateDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // Unique per source wallet: the sender's retry with the same key finds this transfer instead of sending again.
    public Guid IdempotencyKey { get; private set; }

    private Transfer() { }

    public bool Matches(Guid destinationWalletId, Money amount)
    {
        return DestinationWalletId == destinationWalletId && Amount == amount;
    }

    // Changes two aggregates in one transaction, on purpose: an instant transfer must never be half done,
    // and both wallets are in the same database with their own row versions.
    public static TransferExecution Execute(
        Wallet source,
        Wallet destination,
        Money amount,
        ExchangeRate? rate,
        Guid idempotencyKey,
        DateTimeOffset now)
    {
        if (source.Id == destination.Id)
        {
            throw new DomainException("A wallet cannot transfer to itself.");
        }

        Money receivedAmount = GetReceivedAmount(source, destination, amount, rate);

        Transfer transfer = new Transfer
        {
            Id = Guid.CreateVersion7(),
            SourceWalletId = source.Id,
            DestinationWalletId = destination.Id,
            Amount = amount,
            ReceivedAmount = receivedAmount,
            Rate = rate?.Rate,
            RateDate = rate?.Date,
            IdempotencyKey = idempotencyKey,
            CreatedAt = now
        };

        LedgerEntry debit = source.SendTransfer(transfer.Id, amount, now);
        LedgerEntry credit = destination.ReceiveTransfer(transfer.Id, receivedAmount, now);

        return new TransferExecution(transfer, debit, credit);
    }

    private static Money GetReceivedAmount(Wallet source, Wallet destination, Money amount, ExchangeRate? rate)
    {
        if (source.Currency == destination.Currency)
        {
            if (rate is not null)
            {
                throw new DomainException("A transfer between wallets in the same currency takes no exchange rate.");
            }

            return amount;
        }

        if (rate is null)
        {
            throw new DomainException($"A transfer from {source.Currency} to {destination.Currency} needs an exchange rate.");
        }

        if (rate.From != source.Currency || rate.To != destination.Currency)
        {
            throw new DomainException(
                $"The exchange rate is for {rate.From}/{rate.To}, but the transfer is {source.Currency}/{destination.Currency}.");
        }

        Money receivedAmount = rate.Convert(amount);

        // 0.01 EUR at a rate below 1 rounds down to 0.00: the sender would pay and the receiver get nothing.
        // A zero amount itself is left to the ledger, which rejects it with its own message.
        if (amount.Amount > 0 && receivedAmount.Amount == 0)
        {
            throw new DomainException(
                $"{amount.Amount} {amount.Currency} is too small to convert to {destination.Currency}.");
        }

        return receivedAmount;
    }
}