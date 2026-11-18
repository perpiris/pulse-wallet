import { Component, inject, signal } from '@angular/core';
import {
  FieldTree,
  form,
  FormField,
  FormRoot,
  maxLength,
  required,
  TreeValidationResult,
  validate,
} from '@angular/forms/signals';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiError } from '../../core/api-error';
import { CreateWalletRequest, Currency } from '../wallet.models';
import { WalletsApi } from '../wallets-api';

// Same rules as Wallet.Create in the domain. The API stays the authority (it checks again, and also rejects
// control characters, which a text input can't produce anyway); these only give instant feedback.
const FRIENDLY_NAME_MAX_LENGTH = 40;

@Component({
  selector: 'app-create-wallet',
  // FormRoot: the [formRoot] directive on <form>. FormField: the [formField] directive on each input.
  imports: [FormRoot, FormField],
  templateUrl: './create-wallet.html',
})
export class CreateWallet {
  private readonly walletsApi = inject(WalletsApi);
  private readonly router = inject(Router);

  protected readonly currencies: readonly Currency[] = ['EUR', 'USD', 'DKK'];

  // Signal Forms (stable in v22): the form is built around a plain signal holding the data.
  // That signal is the single source of truth; typing in an input writes straight into it.
  // Compared to reactive forms: no FormGroup/FormControl objects, no valueChanges Observables,
  // and the model is fully typed (walletForm.currency can only hold a Currency).
  private readonly model = signal<CreateWalletRequest>({ friendlyName: '', currency: 'EUR' });

  // form(model, schema, options) returns a FieldTree: an object with the same shape as the model,
  // where each field is a function returning its state: walletForm.friendlyName().errors(), .touched(), .value() ...
  protected readonly walletForm = form(
    this.model,
    // The schema: validation rules, declared once per field instead of in the template.
    (path) => {
      required(path.friendlyName, { message: 'Give the wallet a name.' });
      // required() accepts "   "; the API trims and would reject it, so say so here already.
      validate(path.friendlyName, ({ value }) =>
        value() !== '' && value().trim() === ''
          ? { kind: 'blank', message: 'Give the wallet a name.' }
          : undefined,
      );
      maxLength(path.friendlyName, FRIENDLY_NAME_MAX_LENGTH, {
        message: `At most ${FRIENDLY_NAME_MAX_LENGTH} characters.`,
      });
    },
    {
      // What happens on submit. [formRoot] in the template calls this when the form is submitted,
      // but only if every validator passes; otherwise it marks all fields as touched so their errors show.
      submission: {
        action: (field) => this.create(field),
      },
    },
  );

  // Failures that aren't about one field. A plain signal rather than an error returned to the form:
  // a returned error stays until a field changes and keeps the form invalid, so after a network error
  // the same data couldn't be submitted again.
  protected readonly submitError = signal<string | undefined>(undefined);

  // async/await instead of subscribe(): submission actions return a Promise, and
  // firstValueFrom turns the one-response Observable from HttpClient into one.
  private async create(field: FieldTree<CreateWalletRequest>): Promise<TreeValidationResult> {
    this.submitError.set(undefined);
    try {
      const wallet = await firstValueFrom(this.walletsApi.create(field().value()));
      // Straight to the new wallet's page.
      await this.router.navigate(['/wallets', wallet.id]);
      return undefined;
    } catch (error) {
      const apiError = ApiError.of(error);
      // Returned errors are attached to the form like validation errors: this one shows under the
      // name input and clears as soon as the name changes. `submitting()` resets either way.
      if (apiError.status === 409) {
        return { kind: 'server', fieldTree: field.friendlyName, message: apiError.detail };
      }
      this.submitError.set(apiError.detail);
      return undefined;
    }
  }
}
