import { DatePipe, DecimalPipe } from '@angular/common';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
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
import { StaleExchangeRate, staleExchangeRateOf, TransferDetails } from '../transfer.models';
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
  imports: [DatePipe, DecimalPipe, FormRoot, FormField],
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

  // Where the money would go, derived from the form: computed() re-evaluates whenever the model signal changes.
  private readonly destinationId = computed(() => {
    const { recipient, ownWalletId, walletId } = this.model();
    return recipient === 'mine' ? ownWalletId : walletId.trim();
  });

  // Set when the API answered 422 "Exchange rate is stale". Remembers which destination it was for:
  // the rate depends on the destination wallet's currency, so it only applies while that wallet is still chosen.
  private readonly staleRate = signal<
    { destinationId: string; rate: StaleExchangeRate } | undefined
  >(undefined);

  // The warning the template shows: the stale rate, but only for the destination it was returned for.
  protected readonly pendingStaleRate = computed(() => {
    const stale = this.staleRate();
    return stale?.destinationId === this.destinationId() ? stale.rate : undefined;
  });

  // The user's tick in the checkbox. Not part of the form model: it isn't data for the request,
  // it only decides whether the request carries acceptedRateDate.
  protected readonly acceptStaleRate = signal(false);

  // A rough preview for the warning; the API does the real conversion (rounding down to cents, like here).
  protected readonly receivedEstimate = computed(() => {
    const rate = this.pendingStaleRate();
    const amount = Number(this.model().amount);
    if (rate === undefined || !Number.isFinite(amount) || amount <= 0) {
      return undefined;
    }
    return Math.floor(amount * rate.rate * 100) / 100;
  });

  // Send stays disabled until the user has agreed to the stale rate.
  protected readonly waitingForAcceptance = computed(
    () => this.pendingStaleRate() !== undefined && !this.acceptStaleRate(),
  );

  protected onAcceptStaleRateChange(event: Event): void {
    this.acceptStaleRate.set((event.target as HTMLInputElement).checked);
  }

  private async send(field: FieldTree<SendMoneyModel>): Promise<TreeValidationResult> {
    this.submitError.set(undefined);
    const { recipient } = this.model();
    const destinationId = this.destinationId();
    const amount = Number(this.model().amount);

    if (this.lastAttempt?.destinationId !== destinationId || this.lastAttempt.amount !== amount) {
      this.lastAttempt = { destinationId, amount, idempotencyKey: crypto.randomUUID() };
    }

    const { source } = this.data;
    // The same idempotency key is reused for the resend after accepting: the rejected first attempt created nothing.
    const acceptedRate = this.acceptStaleRate() ? this.pendingStaleRate() : undefined;
    const request = {
      sourceWalletId: source.id,
      destinationWalletId: destinationId,
      amount,
      currency: source.currency,
      acceptedRateDate: acceptedRate?.date,
    };

    try {
      this.dialogRef.close(
        await firstValueFrom(this.transfersApi.create(request, this.lastAttempt.idempotencyKey)),
      );
      return undefined;
    } catch (error) {
      const apiError = ApiError.of(error);
      const recipientField = recipient === 'mine' ? field.ownWalletId : field.walletId;
      // Checked before the other 422s: it's not an error on the amount, but a question for the user.
      // Untick the box, so a newer stale rate (if the API returned one) has to be accepted again.
      const staleRate = staleExchangeRateOf(apiError);
      if (staleRate !== undefined) {
        this.staleRate.set({ destinationId, rate: staleRate });
        this.acceptStaleRate.set(false);
        return undefined;
      }
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
