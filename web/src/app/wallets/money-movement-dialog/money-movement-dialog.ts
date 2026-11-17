import { DecimalPipe } from '@angular/common';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import {
  FieldTree,
  form,
  FormField,
  FormRoot,
  pattern,
  required,
  TreeValidationResult,
  validate,
} from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { ApiError } from '../../core/api-error';
import { WalletDetails } from '../wallet.models';
import { WalletsApi } from '../wallets-api';

export type MoneyMovementKind = 'top-up' | 'withdrawal';

export interface MoneyMovementDialogData {
  wallet: WalletDetails;
  kind: MoneyMovementKind;
}

interface AmountModel {
  amount: string;
}

// Same rules as the domain's Money: positive, at most 2 decimals. Amounts are never rounded, only rejected.
const AMOUNT = /^\d+(\.\d{1,2})?$/;

// Opened with the CDK's Dialog service (see WalletSearch.openMoneyDialog), not placed in a template.
// The CDK gives it the dialog behaviour: an overlay with a backdrop, focus moved inside and trapped there,
// Escape to close, focus returned to the button that opened it, role="dialog", and the rest of the page
// hidden from screen readers (aria-hidden) while it's open.
@Component({
  selector: 'app-money-movement-dialog',
  imports: [DecimalPipe, FormRoot, FormField],
  templateUrl: './money-movement-dialog.html',
  host: {
    class:
      'block rounded-2xl border border-slate-200 bg-white p-6 shadow-2xl dark:border-slate-800 dark:bg-slate-900',
  },
})
export class MoneyMovementDialog {
  private readonly walletsApi = inject(WalletsApi);

  // DialogRef<R>: R is what close() hands back to whoever opened the dialog (here: the updated wallet).
  private readonly dialogRef = inject<DialogRef<WalletDetails>>(DialogRef);

  // DIALOG_DATA: whatever the opener passed as `data`. inject() needs the type spelled out: a token carries none.
  protected readonly data = inject<MoneyMovementDialogData>(DIALOG_DATA);

  protected readonly isTopUp = this.data.kind === 'top-up';

  // A string, not a number: <input type="number"> has quirks (scrolling changes it, "1e3" is valid,
  // the decimal separator follows the browser's locale). Text + a pattern gives exact control,
  // and the string is converted only when it's sent.
  private readonly model = signal<AmountModel>({ amount: '' });

  protected readonly amountForm = form(
    this.model,
    (path) => {
      required(path.amount, { message: 'Enter an amount.' });
      pattern(path.amount, AMOUNT, {
        message: 'Use a number with at most 2 decimals, e.g. 25.50.',
      });
      validate(path.amount, ({ value }) =>
        AMOUNT.test(value()) && Number(value()) === 0
          ? { kind: 'positive', message: 'The amount must be more than 0.' }
          : undefined,
      );
    },
    { submission: { action: (field) => this.submit(field) } },
  );

  // The idempotency key of the last attempt. Retrying the same amount (after a network error or a 409)
  // reuses it, so if the first attempt did reach the server, the retry replays it instead of moving money again.
  // A different amount is a different operation and gets a new key: the API rejects a reused key
  // with a different request (422).
  private lastAttempt: { amount: number; idempotencyKey: string } | undefined;

  // Failures that aren't about the amount (network down, 409 conflict, server error). Kept in a plain signal
  // rather than returned to the form: an error returned from the action stays until a field changes, and
  // while it's there the form counts as invalid, so "try again" with the same amount (the whole point of
  // reusing the idempotency key) couldn't be submitted.
  protected readonly submitError = signal<string | undefined>(undefined);

  private async submit(field: FieldTree<AmountModel>): Promise<TreeValidationResult> {
    this.submitError.set(undefined);
    const amount = Number(field.amount().value());
    if (this.lastAttempt?.amount !== amount) {
      this.lastAttempt = { amount, idempotencyKey: crypto.randomUUID() };
    }

    const { wallet } = this.data;
    const request = { amount, currency: wallet.currency };
    const call = this.isTopUp
      ? this.walletsApi.topUp(wallet.id, request, this.lastAttempt.idempotencyKey)
      : this.walletsApi.withdraw(wallet.id, request, this.lastAttempt.idempotencyKey);

    try {
      this.dialogRef.close(await firstValueFrom(call));
      return undefined;
    } catch (error) {
      const apiError = ApiError.of(error);
      // 422 here means "insufficient funds" (or a domain rule on the amount): it belongs next to the amount,
      // and blocking a resubmit until the amount changes is right: the same amount would fail again.
      if (apiError.status === 422) {
        return { kind: 'server', fieldTree: field.amount, message: apiError.detail };
      }
      this.submitError.set(apiError.detail);
      return undefined;
    }
  }

  protected cancel(): void {
    this.dialogRef.close();
  }
}
