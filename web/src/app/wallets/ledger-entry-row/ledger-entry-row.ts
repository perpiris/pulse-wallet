import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { LedgerEntry } from '../wallet.models';

// One transaction row, shared by the wallet card (latest entries) and the history page.
//
// `host` styles the component's own element, <app-ledger-entry-row>. Without it, that element is an
// unstyled inline box around the template. It replaces the @HostBinding decorator, like input() replaced @Input().
@Component({
  selector: 'app-ledger-entry-row',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './ledger-entry-row.html',
  host: { class: 'flex items-center gap-4 py-3' },
})
export class LedgerEntryRow {
  readonly entry = input.required<LedgerEntry>();

  protected readonly incoming = computed(
    () => this.entry().type === 'TopUp' || this.entry().type === 'TransferIn',
  );

  protected readonly description = computed(() => describe(this.entry()));

  // Full class names written out, never built from pieces ('bg-' + colour): Tailwind finds the classes
  // to generate by scanning the source as text, so a name assembled at runtime would have no CSS.
  protected readonly iconClasses = computed(() =>
    this.incoming()
      ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-400'
      : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
  );
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
