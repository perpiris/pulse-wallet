import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateTransferRequest, TransferDetails } from './transfer.models';

@Service()
export class TransfersApi {
  private readonly http = inject(HttpClient);

  // Like top-ups: the caller creates one idempotency key per transfer and sends the same one on a retry,
  // so a transfer whose response got lost isn't sent twice; the API replays the original transfer instead.
  create(request: CreateTransferRequest, idempotencyKey: string): Observable<TransferDetails> {
    return this.http.post<TransferDetails>('/api/transfers', request, {
      headers: new HttpHeaders({ 'Idempotency-Key': idempotencyKey }),
    });
  }
}
