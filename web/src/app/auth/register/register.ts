import { Component, inject, signal } from '@angular/core';
import {
  email,
  FieldTree,
  form,
  FormField,
  FormRoot,
  required,
  TreeValidationResult,
  validate,
} from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { ApiError } from '../../core/api-error';
import { AuthStore } from '../../core/auth/auth-store';

interface RegisterModel {
  email: string;
  password: string;
  confirmPassword: string;
}

@Component({
  selector: 'app-register',
  imports: [FormRoot, FormField, RouterLink],
  templateUrl: './register.html',
})
export class Register {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  private readonly model = signal<RegisterModel>({ email: '', password: '', confirmPassword: '' });

  // The password policy isn't repeated here: it's configured per environment on the API (length only in
  // development, strict in production), so the API's error messages are the single source of the rules.
  protected readonly registerForm = form(
    this.model,
    (path) => {
      required(path.email, { message: 'Enter your email.' });
      email(path.email, { message: 'Enter a valid email address.' });
      required(path.password, { message: 'Choose a password.' });
      required(path.confirmPassword, { message: 'Repeat the password.' });
      validate(path.confirmPassword, ({ value, valueOf }) =>
        value() && value() !== valueOf(path.password)
          ? { kind: 'mismatch', message: "The passwords don't match." }
          : undefined,
      );
    },
    { submission: { action: (field) => this.createAccount(field) } },
  );

  protected readonly submitError = signal<string | undefined>(undefined);

  private async createAccount(field: FieldTree<RegisterModel>): Promise<TreeValidationResult> {
    this.submitError.set(undefined);
    const { email, password } = this.model();
    try {
      await this.authStore.register({ email, password });
      await this.router.navigateByUrl('/wallets');
      return undefined;
    } catch (error) {
      const apiError = ApiError.of(error);
      const fieldErrors = toFieldErrors(apiError, field);
      if (fieldErrors.length === 0) {
        this.submitError.set(apiError.detail);
      }
      return fieldErrors;
    }
  }
}

// ASP.NET Core Identity answers a failed registration with a 400 whose `errors` are keyed by error codes:
// "DuplicateUserName", "InvalidEmail", "PasswordTooShort", "PasswordRequiresDigit", ...
// Each one is shown under the field it's about.
// A plain array (not TreeValidationResult, which can also be a single error or nothing), so the caller can
// check `.length` to tell "errors for the fields" from "nothing field-specific".
interface ServerFieldError {
  kind: 'server';
  fieldTree: FieldTree<string>;
  message: string;
}

function toFieldErrors(error: ApiError, field: FieldTree<RegisterModel>): ServerFieldError[] {
  const errors = Object.entries(error.fieldErrors).flatMap(([code, messages]) => {
    if (code.startsWith('Password')) {
      return messages.map((message) => ({
        kind: 'server' as const,
        fieldTree: field.password,
        message,
      }));
    }
    if (code.startsWith('Duplicate')) {
      // Identity says "Username '...' is already taken"; here the username *is* the email.
      return [
        {
          kind: 'server' as const,
          fieldTree: field.email,
          message: 'An account with this email already exists.',
        },
      ];
    }
    if (code.startsWith('Invalid')) {
      return messages.map((message) => ({
        kind: 'server' as const,
        fieldTree: field.email,
        message,
      }));
    }
    return [];
  });

  // With unique emails, a taken email fails twice (DuplicateUserName and DuplicateEmail): show it once.
  return errors.filter(
    (candidate, index) =>
      errors.findIndex(
        (other) => other.fieldTree === candidate.fieldTree && other.message === candidate.message,
      ) === index,
  );
}
