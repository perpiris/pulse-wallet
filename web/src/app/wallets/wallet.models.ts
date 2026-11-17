// The API's JSON responses, mirroring the C# records in PulseWallet.Api/Contracts.
// Interfaces, not classes: they only describe a shape for the compiler and disappear from the built JavaScript.

// C# enums travel as text ("EUR"), so they become unions of string literals here.
export type Currency = 'EUR' | 'USD' | 'DKK';

export type LedgerEntryType = 'TopUp' | 'Withdrawal' | 'TransferIn' | 'TransferOut';

export interface LedgerEntry {
  id: string;
  type: LedgerEntryType;
  // A C# decimal arrives as a JSON number, a double in JavaScript. Fine for display;
  // don't do money maths with it (0.1 + 0.2 !== 0.3).
  amount: number;
  currency: Currency;
  // JSON has no date type: DateTimeOffset arrives as an ISO 8601 string. The date pipe formats it as is.
  createdAt: string;
  transferId: string | null;
  counterpartyUsername: string | null;
}

export interface WalletDetails {
  id: string;
  username: string;
  currency: Currency;
  balance: number;
  createdAt: string;
  recentEntries: LedgerEntry[];
}
