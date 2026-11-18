import { DatePipe, DecimalPipe, DOCUMENT } from '@angular/common';
import { Component, DestroyRef, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LedgerEntryRow } from '../ledger-entry-row/ledger-entry-row';
import { WalletDetails } from '../wallet.models';

// A presentational component: it only displays the data it's given. It doesn't call the API or know
// where the wallet came from, so it can be reused anywhere a wallet needs showing.
// (Copying the id is still "display" work: it only touches the clipboard, never the server.)
//
// Pipes are imported one by one (DatePipe, DecimalPipe: the `number` pipe) instead of importing CommonModule,
// so the bundle only contains what the template uses.
@Component({
  selector: 'app-wallet-card',
  imports: [DatePipe, DecimalPipe, RouterLink, LedgerEntryRow],
  templateUrl: './wallet-card.html',
})
export class WalletCard {
  private readonly document = inject(DOCUMENT);

  // input() replaces the @Input() decorator. It returns a signal, so read it as `this.wallet()`.
  // `required` makes the compiler reject `<app-wallet-card />` without [wallet].
  readonly wallet = input.required<WalletDetails>();

  // What the button says: "Copy ID", then "Copied" or "Couldn't copy" for two seconds.
  protected readonly copyState = signal<'idle' | 'copied' | 'failed'>('idle');

  private resetTimer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    // DestroyRef: register cleanup for when the component goes away, so a pending timer doesn't
    // fire on a destroyed component. The modern replacement for implementing ngOnDestroy.
    inject(DestroyRef).onDestroy(() => clearTimeout(this.resetTimer));
  }

  // The wallet id is what someone needs to send money here (Send → "Someone else's wallet").
  // The Clipboard API is async, only exists on secure pages (https, or localhost in development), and can be
  // refused (permission blocked, or the page isn't focused). A refusal must be visible, not a dead button.
  protected async copyId(): Promise<void> {
    try {
      const clipboard = this.document.defaultView?.navigator.clipboard;
      if (!clipboard) {
        throw new Error('Clipboard API not available');
      }
      await clipboard.writeText(this.wallet().id);
      this.copyState.set('copied');
    } catch {
      this.copyState.set('failed');
    }
    clearTimeout(this.resetTimer);
    this.resetTimer = setTimeout(() => this.copyState.set('idle'), 2000);
  }
}
