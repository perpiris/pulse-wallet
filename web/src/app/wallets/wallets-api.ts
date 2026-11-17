import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateWalletRequest,
  LedgerEntriesPage,
  LedgerEntriesQuery,
  MoneyMovementRequest,
  WalletDetails,
} from './wallet.models';

// @Service() (v22) is the short form of @Injectable({ providedIn: 'root' }): one shared instance
// for the whole app, created the first time something inject()s it.
//
// The class only talks HTTP. Components never call HttpClient directly, so the URLs and
// response types for wallets live in one place.
@Service()
export class WalletsApi {
  private readonly http = inject(HttpClient);

  // HttpClient returns an Observable that's "cold": nothing is sent until something subscribes
  // (here, the rxResource in WalletSearch). `get<WalletDetails>` only tells TypeScript the shape;
  // nothing checks at runtime that the JSON really looks like that.
  getById(id: string): Observable<WalletDetails> {
    return this.http.get<WalletDetails>(`/api/wallets/${id}`);
  }

  create(request: CreateWalletRequest): Observable<WalletDetails> {
    return this.http.post<WalletDetails>('/api/wallets', request);
  }

  getLedgerEntries(id: string, query: LedgerEntriesQuery): Observable<LedgerEntriesPage> {
    // HttpParams is immutable: set() returns a new instance, it doesn't change this one.
    // Filters that aren't set are left out of the URL entirely, so the API sees "no filter", not an empty value.
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value) {
        params = params.set(key, value);
      }
    }
    return this.http.get<LedgerEntriesPage>(`/api/wallets/${id}/ledger-entries`, { params });
  }

  // Money-moving calls take an idempotency key: the caller creates one per operation and sends the same one
  // on a retry, so a request that reached the server but whose response got lost can't move money twice.
  topUp(
    id: string,
    request: MoneyMovementRequest,
    idempotencyKey: string,
  ): Observable<WalletDetails> {
    return this.http.post<WalletDetails>(`/api/wallets/${id}/top-ups`, request, {
      headers: idempotencyHeaders(idempotencyKey),
    });
  }

  withdraw(
    id: string,
    request: MoneyMovementRequest,
    idempotencyKey: string,
  ): Observable<WalletDetails> {
    return this.http.post<WalletDetails>(`/api/wallets/${id}/withdrawals`, request, {
      headers: idempotencyHeaders(idempotencyKey),
    });
  }
}

function idempotencyHeaders(idempotencyKey: string): HttpHeaders {
  return new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
}
