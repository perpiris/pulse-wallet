import { Component } from '@angular/core';
import { IsActiveMatchOptions, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
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
  // `{ exact: true }` would also compare query params, so "/?id=..." wouldn't count as "/".
  // Exact on the path only: "/" is active on the lookup page with or without ?id, and not on /wallets/new.
  protected readonly exactPath: IsActiveMatchOptions = {
    paths: 'exact',
    queryParams: 'ignored',
    fragment: 'ignored',
    matrixParams: 'ignored',
  };
}
