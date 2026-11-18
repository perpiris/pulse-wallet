import { DecimalPipe } from '@angular/common';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import {
  FieldTree,
  form,
  FormField,
  FormRoot,
  required,
  TreeValidationResult,
  validate,
} from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { ApiError } from '../../core/api-error';
import { amountRules } from '../../wallets/amount-rules';
import { WalletDetails, WalletSummary } from '../../wallets/wallet.models';
import { TransferDetails } from '../transfer.models';
import { TransfersApi } from '../transfers-api';

export interface SendMoneyDialogData {
  source: WalletDetails;
  // The user's other wallets in the same currency: the only own wallets a transfer can go to.
  ownWallets: WalletSummary[];
}

type Recipient = 'mine' | 'other';

interface SendMoneyModel {
  recipient: Recipient;
  ownWalletId: string;
  walletId: string;
  amount: string;
}

const WALLET_ID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// Sends money from one wallet to another: one of your own, or anyone's by pasting their wallet ID.
@Component({
  selector: 'app-send-money-dialog',
  imports: [DecimalPipe, FormRoot, FormField],
  templateUrl: './send-money-dialog.html',
  host: {
    class:
      'block rounded-2xl border border-slate-200 bg-white p-6 shadow-2xl dark:border-slate-800 dark:bg-slate-900',
  },
})
export class SendMoneyDialog {
  private readonly transfersApi = inject(TransfersApi);
  private readonly dialogRef = inject<DialogRef<TransferDetails>>(DialogRef);
  protected readonly data = inject<SendMoneyDialogData>(DIALOG_DATA);

  private readonly model = signal<SendMoneyModel>({
    recipient: this.data.ownWallets.length > 0 ? 'mine' : 'other',
    ownWalletId: this.data.ownWallets[0]?.id ?? '',
    walletId: '',
    amount: '',
  });

  protected readonly sendForm = form(
    this.model,
    (path) => {
      // `when`: the rule only applies while it returns true. Each recipient field is checked only
      // when that option is chosen, so a leftover value in the hidden field never blocks the form.
      required(path.ownWalletId, {
        message: 'Choose one of your wallets.',
        when: ({ valueOf }) => valueOf(path.recipient) === 'mine',
      });
      validate(path.walletId, ({ value, valueOf }) => {
        if (valueOf(path.recipient) !== 'other') {
          return undefined;
        }
        const walletId = value().trim();
        if (walletId === '') {
          return { kind: 'required', message: 'Paste the wallet ID you want to send to.' };
        }
        if (!WALLET_ID.test(walletId)) {
          return { kind: 'format', message: "That doesn't look like a wallet ID." };
        }
        if (walletId.toLowerCase() === this.data.source.id.toLowerCase()) {
          return { kind: 'self', message: "That's the wallet you're sending from." };
        }
        return undefined;
      });
      amountRules(path.amount);
    },
    { submission: { action: (field) => this.send(field) } },
  );

  // One key per transfer (recipient + amount). Retrying the same transfer reuses it, so a first attempt that
  // did reach the server is replayed rather than sent twice; changing the recipient or amount is a new transfer.
  private lastAttempt:
    { destinationId: string; amount: number; idempotencyKey: string } | undefined;

  // Failures that aren't about one field (network, 409, server error): a signal, not a form error, so the
  // same transfer can be submitted again (see money-movement-dialog.ts for why).
  protected readonly submitError = signal<string | undefined>(undefined);

  private async send(field: FieldTree<SendMoneyModel>): Promise<TreeValidationResult> {
    this.submitError.set(undefined);
    const { recipient, ownWalletId, walletId } = this.model();
    const destinationId = recipient === 'mine' ? ownWalletId : walletId.trim();
    const amount = Number(this.model().amount);

    if (this.lastAttempt?.destinationId !== destinationId || this.lastAttempt.amount !== amount) {
      this.lastAttempt = { destinationId, amount, idempotencyKey: crypto.randomUUID() };
    }

    const { source } = this.data;
    const request = {
      sourceWalletId: source.id,
      destinationWalletId: destinationId,
      amount,
      currency: source.currency,
    };

    try {
      this.dialogRef.close(
        await firstValueFrom(this.transfersApi.create(request, this.lastAttempt.idempotencyKey)),
      );
      return undefined;
    } catch (error) {
      const apiError = ApiError.of(error);
      const recipientField = recipient === 'mine' ? field.ownWalletId : field.walletId;
      // 422: insufficient funds → the amount. 404: no such destination wallet. 400: a domain rule about the pair
      // (different currency, same wallet) → the recipient. Anything else isn't about a field.
      if (apiError.status === 422) {
        return { kind: 'server', fieldTree: field.amount, message: apiError.detail };
      }
      if (apiError.status === 404) {
        return { kind: 'server', fieldTree: recipientField, message: 'No wallet with this ID.' };
      }
      if (apiError.status === 400) {
        return { kind: 'server', fieldTree: recipientField, message: apiError.detail };
      }
      this.submitError.set(apiError.detail);
      return undefined;
    }
  }

  protected cancel(): void {
    this.dialogRef.close();
  }
}
