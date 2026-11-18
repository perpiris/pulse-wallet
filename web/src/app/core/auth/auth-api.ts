import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';

export interface Credentials {
  email: string;
  password: string;
}

// What POST /api/auth/login and /refresh return (ASP.NET Core Identity's AccessTokenResponse).
// The tokens are opaque strings: ASP.NET Core's own encrypted format, not JWTs, so there's nothing to decode.
export interface TokenResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

// The HTTP calls to the Identity endpoints the API exposes under /api/auth.
@Service()
export class AuthApi {
  private readonly http = inject(HttpClient);

  register(credentials: Credentials): Observable<void> {
    return this.http.post<void>('/api/auth/register', credentials);
  }

  login(credentials: Credentials): Observable<TokenResponse> {
    return this.http.post<TokenResponse>('/api/auth/login', credentials);
  }

  refresh(refreshToken: string): Observable<TokenResponse> {
    return this.http.post<TokenResponse>('/api/auth/refresh', { refreshToken });
  }
}
