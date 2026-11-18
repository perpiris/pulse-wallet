import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthStore } from './auth-store';

// Endpoints that must never carry a token or trigger a refresh: they're how you get a token in the first place.
const PUBLIC_AUTH_URLS = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh'];

// 1. Adds "Authorization: Bearer <token>" to our own API calls (never to other hosts: the token is ours to protect).
// 2. On a 401, refreshes the tokens once and retries the request, so an expired access token is invisible to the user.
// 3. If the refresh fails too, the session is over: sign out and go to the login page, remembering where we were.
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/') || PUBLIC_AUTH_URLS.includes(request.url)) {
    return next(request);
  }

  const authStore = inject(AuthStore);
  const router = inject(Router);

  return next(withToken(request, authStore.accessToken())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      // from(): turns the refresh Promise into an Observable, so it can be chained with the retry.
      // switchMap: when the refresh finishes, switch to the retried request (or rethrow the original 401).
      return from(authStore.refresh()).pipe(
        switchMap((refreshed) => {
          if (refreshed) {
            return next(withToken(request, authStore.accessToken()));
          }
          void authStore.signOut(router.url);
          return throwError(() => error);
        }),
      );
    }),
  );
};

// Requests are immutable: clone() makes a copy with the extra header.
function withToken(request: HttpRequest<unknown>, token: string | undefined): HttpRequest<unknown> {
  return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
}
