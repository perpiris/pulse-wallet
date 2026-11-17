import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { ThemeToggle } from './core/theme-toggle/theme-toggle';

// The root component: the page shell. The current route's component renders inside <router-outlet />.
//
// File naming follows the current style guide (v20+): `app.ts` and class `App`,
// not `app.component.ts` and `AppComponent`. Same for services: `wallets-api.ts`, not `wallets-api.service.ts`.
@Component({
  selector: 'app-root',
  // RouterLink: `routerLink="/"` navigates inside the app without reloading the page (unlike a plain href).
  imports: [RouterOutlet, RouterLink, ThemeToggle],
  templateUrl: './app.html',
})
export class App {}
