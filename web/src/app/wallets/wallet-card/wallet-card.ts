import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { LedgerEntry, WalletDetails } from '../wallet.models';

interface EntryRow {
  id: string;
  description: string;
  incoming: boolean;
  amount: number;
  currency: string;
  createdAt: string;
}

// A presentational component: it only displays the data it's given. It doesn't call the API or know
// where the wallet came from, so it can be reused anywhere a wallet needs showing.
//
// Pipes are imported one by one (CurrencyPipe, DatePipe) instead of importing CommonModule,
// so the bundle only contains what the template uses.
@Component({
  selector: 'app-wallet-card',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './wallet-card.html',
})
export class WalletCard {
  // input() replaces the @Input() decorator. It returns a signal, so read it as `this.wallet()`.
  // `required` makes the compiler reject `<app-wallet-card />` without [wallet].
  readonly wallet = input.required<WalletDetails>();

  // Derived from the input: recalculated only when a new wallet comes in.
  // Keeps the template free of logic (no method calls that would run on every render).
  protected readonly entries = computed(() => this.wallet().recentEntries.map(toRow));

  // Full class names written out, never built from pieces ('bg-' + colour): Tailwind finds the classes
  // to generate by scanning the source as text, so a name assembled at runtime would have no CSS.
  protected readonly incomingIcon =
    'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400';
  protected readonly outgoingIcon =
    'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300';
}

function toRow(entry: LedgerEntry): EntryRow {
  return {
    id: entry.id,
    description: describe(entry),
    incoming: entry.type === 'TopUp' || entry.type === 'TransferIn',
    amount: entry.amount,
    currency: entry.currency,
    createdAt: entry.createdAt,
  };
}

// No `default` branch on purpose: `type` is a union of four strings, so TypeScript knows this switch
// covers every case. If the API adds a fifth type to the union, this function stops compiling.
function describe(entry: LedgerEntry): string {
  switch (entry.type) {
    case 'TopUp':
      return 'Top-up';
    case 'Withdrawal':
      return 'Withdrawal';
    case 'TransferIn':
      return `From @${entry.counterpartyUsername}`;
    case 'TransferOut':
      return `To @${entry.counterpartyUsername}`;
  }
}
