import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth-store';

// Functional guards (v15+): plain functions instead of classes implementing CanActivate.
// Returning a UrlTree (router.createUrlTree) redirects; returning true lets the navigation through.
// Guards are UX, not security: they only decide which page to show. The API checks every request anyway.

// For pages that need a signed-in user. Remembers the requested URL so login can send you back to it.
export const authGuard: CanActivateFn = (_route, state) => {
  if (inject(AuthStore).isSignedIn()) {
    return true;
  }
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

// For login and register: a signed-in user has no business there, so go to their wallets.
export const guestGuard: CanActivateFn = () => {
  if (!inject(AuthStore).isSignedIn()) {
    return true;
  }
  return inject(Router).createUrlTree(['/wallets']);
};
