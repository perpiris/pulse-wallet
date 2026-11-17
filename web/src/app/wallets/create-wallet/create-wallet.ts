import { Component, inject, signal } from '@angular/core';
import {
  FieldTree,
  form,
  FormField,
  FormRoot,
  maxLength,
  minLength,
  pattern,
  required,
  TreeValidationResult,
} from '@angular/forms/signals';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiError } from '../../core/api-error';
import { CreateWalletRequest, Currency } from '../wallet.models';
import { WalletsApi } from '../wallets-api';

// Same rules as Wallet.Create in the domain. The API stays the authority (it checks again);
// these only give instant feedback instead of a round trip.
const USERNAME_MIN_LENGTH = 3;
const USERNAME_MAX_LENGTH = 20;
const USERNAME_CHARACTERS = /^[a-zA-Z0-9_]*$/;

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
  private readonly model = signal<CreateWalletRequest>({ username: '', currency: 'EUR' });

  // form(model, schema, options) returns a FieldTree: an object with the same shape as the model,
  // where each field is a function returning its state: walletForm.username().errors(), .touched(), .value() ...
  protected readonly walletForm = form(
    this.model,
    // The schema: validation rules, declared once per field instead of in the template.
    (path) => {
      required(path.username, { message: 'Choose a username.' });
      minLength(path.username, USERNAME_MIN_LENGTH, {
        message: `At least ${USERNAME_MIN_LENGTH} characters.`,
      });
      maxLength(path.username, USERNAME_MAX_LENGTH, {
        message: `At most ${USERNAME_MAX_LENGTH} characters.`,
      });
      pattern(path.username, USERNAME_CHARACTERS, {
        message: 'Only letters, digits and underscores.',
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

  // async/await instead of subscribe(): submission actions return a Promise, and
  // firstValueFrom turns the one-response Observable from HttpClient into one.
  private async create(field: FieldTree<CreateWalletRequest>): Promise<TreeValidationResult> {
    try {
      const wallet = await firstValueFrom(this.walletsApi.create(field().value()));
      // Navigate to the lookup page with ?id=..., which shows the new wallet's card.
      await this.router.navigate(['/'], { queryParams: { id: wallet.id } });
      return undefined;
    } catch (error) {
      // Returned errors are attached to the form like validation errors: a field-level one shows next
      // to that input, one without `fieldTree` belongs to the whole form. `submitting()` resets either way.
      return toFormErrors(error, field);
    }
  }
}

function toFormErrors(error: unknown, field: FieldTree<CreateWalletRequest>): TreeValidationResult {
  if (!(error instanceof ApiError)) {
    return { kind: 'server', message: ApiError.unexpected().detail };
  }

  if (error.status === 409) {
    return { kind: 'server', fieldTree: field.username, message: error.detail };
  }

  return { kind: 'server', message: error.detail };
}
