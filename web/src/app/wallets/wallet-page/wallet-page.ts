import { Dialog } from '@angular/cdk/dialog';
import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiError } from '../../core/api-error';
import {
  SendMoneyDialog,
  SendMoneyDialogData,
} from '../../transfers/send-money-dialog/send-money-dialog';
import { TransferDetails } from '../../transfers/transfer.models';
import {
  MoneyMovementDialog,
  MoneyMovementDialogData,
  MoneyMovementKind,
} from '../money-movement-dialog/money-movement-dialog';
import { WalletCard } from '../wallet-card/wallet-card';
import { WalletDetails, WalletSummary } from '../wallet.models';
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

  // True while the list of the user's wallets loads, between clicking "Send" and the dialog opening.
  protected readonly preparingSend = signal(false);

  // The dialog needs the user's other wallets in the same currency (the only own wallets a transfer can go to),
  // so they're loaded right before it opens: always fresh, and never fetched if nobody clicks "Send".
  protected async openSendDialog(): Promise<void> {
    if (!this.wallet.hasValue() || this.preparingSend()) {
      return;
    }
    const source = this.wallet.value();

    this.preparingSend.set(true);
    let ownWallets: WalletSummary[];
    try {
      const wallets = await firstValueFrom(this.walletsApi.list());
      ownWallets = wallets.filter((w) => w.id !== source.id && w.currency === source.currency);
    } catch {
      // The dialog still works without the list: you can paste a wallet ID.
      ownWallets = [];
    } finally {
      this.preparingSend.set(false);
    }

    const dialogRef = this.dialog.open<TransferDetails, SendMoneyDialogData>(SendMoneyDialog, {
      data: { source, ownWallets },
      ariaLabelledBy: 'send-money-title',
      width: 'calc(100% - 2rem)',
      maxWidth: '28rem',
      backdropClass: ['bg-slate-950/60', 'backdrop-blur-sm'],
    });

    // The API answers a transfer with the transfer, not the wallet, so the wallet is fetched again for the new
    // balance and entries. Fetched and set by hand rather than reload(): reload() would flip isLoading() and
    // flash the skeleton over a card that's already on screen.
    dialogRef.closed.subscribe(async (transfer) => {
      if (transfer) {
        this.wallet.set(await firstValueFrom(this.walletsApi.getById(source.id)));
      }
    });
  }

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
