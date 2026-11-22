import { ApiError } from '../core/api-error';
import { Currency } from '../wallets/wallet.models';

// POST /api/transfers. amount + currency is what the sender pays, in the source wallet's currency;
// if the destination wallet uses another currency, the API converts it.
export interface CreateTransferRequest {
  sourceWalletId: string;
  destinationWalletId: string;
  amount: number;
  currency: Currency;
  // Only after a 422 "Exchange rate is stale": the date of the rate the user agreed to (ISO date).
  // The `?` makes the property optional, so a normal transfer simply leaves it out.
  acceptedRateDate?: string;
}

// The API's TransferResponse.
export interface TransferDetails {
  id: string;
  sourceWalletId: string;
  destinationWalletId: string;
  amount: number;
  currency: Currency;
  // What the receiver got: equal to amount/currency unless the wallets use different currencies.
  receivedAmount: number;
  receivedCurrency: Currency;
  // The rate used and the day it was published (ISO date); null when no conversion happened.
  rate: number | null;
  rateDate: string | null;
  createdAt: string;
}

// The `exchangeRate` extension on a 422 "Exchange rate is stale": the old rate the API would use, if the user agrees.
export interface StaleExchangeRate {
  from: Currency;
  to: Currency;
  rate: number;
  // ISO date (yyyy-MM-dd): when the rate was published. Sent back as acceptedRateDate.
  date: string;
}

// Returns the stale rate when the error is that 422, otherwise undefined. The body comes from the network,
// so every field is checked before TypeScript is told it's a StaleExchangeRate.
export function staleExchangeRateOf(error: ApiError): StaleExchangeRate | undefined {
  if (error.status !== 422) {
    return undefined;
  }
  const value = error.problem['exchangeRate'];
  if (typeof value !== 'object' || value === null) {
    return undefined;
  }
  const { from, to, rate, date } = value as Record<string, unknown>;
  if (
    typeof from !== 'string' ||
    typeof to !== 'string' ||
    typeof rate !== 'number' ||
    typeof date !== 'string'
  ) {
    return undefined;
  }
  return { from: from as Currency, to: to as Currency, rate, date };
}
