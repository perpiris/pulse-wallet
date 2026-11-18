import { Component, inject, input, signal } from '@angular/core';
import {
  email,
  form,
  FormField,
  FormRoot,
  required,
  TreeValidationResult,
} from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { ApiError } from '../../core/api-error';
import { AuthStore } from '../../core/auth/auth-store';
import { Credentials } from '../../core/auth/auth-api';

@Component({
  selector: 'app-login',
  imports: [FormRoot, FormField, RouterLink],
  templateUrl: './login.html',
})
export class Login {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  // ?returnUrl=... set by authGuard: the page the user tried to open before being sent here.
  readonly returnUrl = input<string>();

  private readonly model = signal<Credentials>({ email: '', password: '' });

  // Only "is something there" checks: the password rules are the API's business (and differ per environment),
  // and a login form that judges password strength would just leak the policy for nothing.
  protected readonly loginForm = form(
    this.model,
    (path) => {
      required(path.email, { message: 'Enter your email.' });
      email(path.email, { message: 'Enter a valid email address.' });
      required(path.password, { message: 'Enter your password.' });
    },
    { submission: { action: () => this.signIn() } },
  );

  protected readonly submitError = signal<string | undefined>(undefined);

  private async signIn(): Promise<TreeValidationResult> {
    this.submitError.set(undefined);
    try {
      await this.authStore.login(this.model());
      await this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
    } catch (error) {
      this.submitError.set(loginErrorMessage(ApiError.of(error)));
    }
    return undefined;
  }
}

// Wrong email and wrong password get the same message on purpose: saying which one was wrong would tell an
// attacker which emails have accounts. Lockout (5 failed attempts) is the one case worth naming.
function loginErrorMessage(error: ApiError): string {
  if (error.status === 401) {
    return error.detail === 'LockedOut'
      ? 'Too many failed attempts. Try again in a few minutes.'
      : 'Email or password is incorrect.';
  }
  return error.detail;
}

// Only same-site paths: "/wallets" is fine, but "https://evil.example" or "//evil.example" (a protocol-relative URL)
// would turn the login page into an open redirect that phishing links could abuse.
function safeReturnUrl(returnUrl: string | undefined): string {
  return returnUrl?.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/wallets';
}
