import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiError } from './api-error';

// An interceptor sits between HttpClient and the network, for every request: like a DelegatingHandler
// on a .NET HttpClient (a client-side pipeline, the mirror image of ASP.NET Core middleware).
// This one turns every failed response into an ApiError, so components handle one error type
// instead of digging through HttpErrorResponse. Registered in app.config.ts.
export const problemDetailsInterceptor: HttpInterceptorFn = (request, next) => {
  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        return throwError(() => ApiError.from(error));
      }
      return throwError(() => error);
    }),
  );
};
