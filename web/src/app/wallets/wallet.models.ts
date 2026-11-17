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

export type LedgerEntryDirection = 'Incoming' | 'Outgoing';

// One page of history. `nextCursor` is opaque: send it back as `before` to get the next page; null means no more pages.
export interface LedgerEntriesPage {
  items: LedgerEntry[];
  nextCursor: string | null;
}

export interface WalletDetails {
  id: string;
  username: string;
  currency: Currency;
  balance: number;
  createdAt: string;
  recentEntries: LedgerEntry[];
}

// Request bodies. Same rule as the C# side: requests and responses are separate types,
// even when they overlap, so one can change without the other.
export interface CreateWalletRequest {
  username: string;
  currency: Currency;
}

// Query string for GET /api/wallets/{id}/ledger-entries. Dates are "yyyy-MM-dd", like C#'s DateOnly.
export interface LedgerEntriesQuery {
  from?: string;
  to?: string;
  direction?: LedgerEntryDirection;
  before?: string;
}

// Body of POST /api/wallets/{id}/top-ups and /withdrawals. The currency must be the wallet's own.
export interface MoneyMovementRequest {
  amount: number;
  currency: Currency;
}
