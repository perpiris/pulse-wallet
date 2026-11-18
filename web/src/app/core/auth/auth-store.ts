import { computed, inject, Service, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthApi, Credentials, TokenResponse } from './auth-api';

interface Session {
  email: string;
  accessToken: string;
  refreshToken: string;
}

const STORAGE_KEY = 'pulsewallet-session';

// Who is signed in, and their tokens. The rest of the app only reads `isSignedIn`, `email` and `accessToken`.
//
// Where tokens live is a security trade-off:
// - sessionStorage (used here): survives a page reload, gone when the tab closes, not shared between tabs.
//   But any script running on the page can read it, so an XSS bug would leak the tokens.
// - Memory only: safer against XSS, but every reload signs you out.
// - The production-grade answer for a bank is a BFF (backend-for-frontend): the server keeps the tokens and
//   the browser only gets an httpOnly cookie that JavaScript can't read at all. See ANGULAR.md.
@Service()
export class AuthStore {
  private readonly authApi = inject(AuthApi);
  private readonly router = inject(Router);

  private readonly session = signal<Session | undefined>(readStoredSession());

  readonly isSignedIn = computed(() => this.session() !== undefined);
  readonly email = computed(() => this.session()?.email);
  readonly accessToken = computed(() => this.session()?.accessToken);

  // While a refresh is running, every other 401 waits for the same one instead of starting its own.
  // Two parallel refreshes would race: the API rotates refresh tokens, so the second would use a stale one.
  private refreshInFlight: Promise<boolean> | undefined;

  async login(credentials: Credentials): Promise<void> {
    const tokens = await firstValueFrom(this.authApi.login(credentials));
    this.start(credentials.email, tokens);
  }

  // Register doesn't sign you in (it returns no tokens), so this logs in right after.
  async register(credentials: Credentials): Promise<void> {
    await firstValueFrom(this.authApi.register(credentials));
    await this.login(credentials);
  }

  // Called by the auth interceptor when a request comes back 401 (the access token expired after an hour).
  // Resolves to true if new tokens were obtained, false if the session is over.
  refresh(): Promise<boolean> {
    this.refreshInFlight ??= this.doRefresh().finally(() => (this.refreshInFlight = undefined));
    return this.refreshInFlight;
  }

  // Bearer tokens can't be revoked by the client: signing out means forgetting them.
  // They stay technically valid until they expire, which is one more reason for short-lived access tokens.
  async signOut(returnUrl?: string): Promise<void> {
    this.end();
    await this.router.navigate(['/login'], { queryParams: returnUrl ? { returnUrl } : {} });
  }

  private async doRefresh(): Promise<boolean> {
    const current = this.session();
    if (current === undefined) {
      return false;
    }

    try {
      const tokens = await firstValueFrom(this.authApi.refresh(current.refreshToken));
      this.start(current.email, tokens);
      return true;
    } catch {
      this.end();
      return false;
    }
  }

  private start(email: string, tokens: TokenResponse): void {
    const session: Session = {
      email,
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
    };
    this.session.set(session);
    writeStoredSession(session);
  }

  private end(): void {
    this.session.set(undefined);
    writeStoredSession(undefined);
  }
}

// sessionStorage can throw (blocked storage, some private modes): then the session just isn't remembered.
function readStoredSession(): Session | undefined {
  try {
    const stored = sessionStorage.getItem(STORAGE_KEY);
    return stored ? (JSON.parse(stored) as Session) : undefined;
  } catch {
    return undefined;
  }
}

function writeStoredSession(session: Session | undefined): void {
  try {
    if (session) {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } else {
      sessionStorage.removeItem(STORAGE_KEY);
    }
  } catch {
    // Not remembered across reloads; the in-memory session still works.
  }
}
