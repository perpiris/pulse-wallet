import { Currency } from '../wallets/wallet.models';

// POST /api/transfers. Both wallets must use the same currency (cross-currency is #4 on the API side).
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
  createdAt: string;
}
