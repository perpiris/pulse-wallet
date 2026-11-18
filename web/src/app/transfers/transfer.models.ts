import { Currency } from '../wallets/wallet.models';

// POST /api/transfers. amount + currency is what the sender pays, in the source wallet's currency;
// if the destination wallet uses another currency, the API converts it.
export interface CreateTransferRequest {
  sourceWalletId: string;
  destinationWalletId: string;
  amount: number;
  currency: Currency;
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
