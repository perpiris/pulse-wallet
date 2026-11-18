import { Component, inject } from '@angular/core';
import { IsActiveMatchOptions, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthStore } from './core/auth/auth-store';
import { ThemeToggle } from './core/theme-toggle/theme-toggle';

// The root component: the page shell. The current route's component renders inside <router-outlet />.
//
// File naming follows the current style guide (v20+): `app.ts` and class `App`,
// not `app.component.ts` and `AppComponent`. Same for services: `wallets-api.ts`, not `wallets-api.service.ts`.
@Component({
  selector: 'app-root',
  // RouterLink: `routerLink="/"` navigates inside the app without reloading the page (unlike a plain href).
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ThemeToggle],
  templateUrl: './app.html',
})
export class App {
  // The header shows different links signed in and signed out; it reads the store's signals directly.
  protected readonly authStore = inject(AuthStore);

  // Exact on the path only: "My wallets" (/wallets) isn't also active on /wallets/new or /wallets/{id}.
  // `{ exact: true }` would do the same but also compare query params, which a link like ?returnUrl= would break.
  protected readonly exactPath: IsActiveMatchOptions = {
    paths: 'exact',
    queryParams: 'ignored',
    fragment: 'ignored',
    matrixParams: 'ignored',
  };

  protected signOut(): void {
    void this.authStore.signOut();
  }
}
