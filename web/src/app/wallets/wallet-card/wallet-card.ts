import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LedgerEntryRow } from '../ledger-entry-row/ledger-entry-row';
import { WalletDetails } from '../wallet.models';

// A presentational component: it only displays the data it's given. It doesn't call the API or know
// where the wallet came from, so it can be reused anywhere a wallet needs showing.
//
// Pipes are imported one by one (DatePipe, DecimalPipe: the `number` pipe) instead of importing CommonModule,
// so the bundle only contains what the template uses.
@Component({
  selector: 'app-wallet-card',
  imports: [DatePipe, DecimalPipe, RouterLink, LedgerEntryRow],
  templateUrl: './wallet-card.html',
})
export class WalletCard {
  // input() replaces the @Input() decorator. It returns a signal, so read it as `this.wallet()`.
  // `required` makes the compiler reject `<app-wallet-card />` without [wallet].
  readonly wallet = input.required<WalletDetails>();
}
