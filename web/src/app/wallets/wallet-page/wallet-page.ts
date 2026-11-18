import { Dialog } from '@angular/cdk/dialog';
import { Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { ApiError } from '../../core/api-error';
import {
  MoneyMovementDialog,
  MoneyMovementDialogData,
  MoneyMovementKind,
} from '../money-movement-dialog/money-movement-dialog';
import { WalletCard } from '../wallet-card/wallet-card';
import { WalletDetails } from '../wallet.models';
import { WalletsApi } from '../wallets-api';

// One wallet: its card, plus the actions you can take on it.
@Component({
  selector: 'app-wallet-page',
  imports: [RouterLink, WalletCard],
  templateUrl: './wallet-page.html',
})
export class WalletPage {
  private readonly walletsApi = inject(WalletsApi);
  private readonly dialog = inject(Dialog);

  // The :id route parameter (route 'wallets/:id'), delivered by withComponentInputBinding.
  readonly id = input.required<string>();

  // A resource loads async data whenever its `params` signal changes and exposes the result as signals:
  // value(), isLoading(), error(), hasValue(). No subscribe(), no unsubscribe, no loading flags by hand.
  // Navigating from one wallet to another reuses this component: the new id cancels the old request.
  protected readonly wallet = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.walletsApi.getById(params),
  });

  // Someone else's wallet comes back as a 404, exactly like a wallet that doesn't exist.
  protected readonly error = computed(() => {
    const error = this.wallet.error();
    return error === undefined ? undefined : ApiError.of(error);
  });

  protected openMoneyDialog(kind: MoneyMovementKind): void {
    if (!this.wallet.hasValue()) {
      return;
    }

    // open<R, D>: R is what the dialog closes with, D the data it receives.
    // The classes style the CDK's own elements: the dimmed backdrop and the pane that holds the dialog.
    const dialogRef = this.dialog.open<WalletDetails, MoneyMovementDialogData>(
      MoneyMovementDialog,
      {
        data: { wallet: this.wallet.value(), kind },
        ariaLabelledBy: 'money-dialog-title',
        width: 'calc(100% - 2rem)',
        maxWidth: '28rem',
        backdropClass: ['bg-slate-950/60', 'backdrop-blur-sm'],
      },
    );

    // `closed` emits once, with the updated wallet or undefined (cancel, Escape, backdrop click), then completes,
    // so this subscription ends on its own: no unsubscribe needed.
    dialogRef.closed.subscribe((updated) => {
      if (updated) {
        // A resource's value can be set by hand: the API already returned the new balance and entries,
        // so the card updates without fetching the wallet again.
        this.wallet.set(updated);
      }
    });
  }
}
