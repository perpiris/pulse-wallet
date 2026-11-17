import { HttpErrorResponse } from '@angular/common/http';

// The JSON body ASP.NET Core sends for errors (RFC 9457). Everything is optional: a proxy or a crash
// can return an error without this body, so nothing here can be relied on.
interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

// The one error type the app works with. `readonly status: number` in the constructor is a parameter
// property: TypeScript declares the field and assigns it in one go (like a C# primary constructor).
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly title: string,
    readonly detail: string,
    readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(detail);
  }

  // For catch blocks and resource errors, which are typed `unknown`: the interceptor already turned
  // HTTP failures into ApiError, so anything else is a bug in our own code and gets a generic message.
  static of(error: unknown): ApiError {
    return error instanceof ApiError ? error : ApiError.unexpected();
  }

  static unexpected(): ApiError {
    return new ApiError(0, 'Unexpected error', 'Something went wrong. Please try again.');
  }

  static from(response: HttpErrorResponse): ApiError {
    // Status 0 means no response at all: the server is down, the network is gone, or the request was blocked.
    if (response.status === 0) {
      return new ApiError(
        0,
        'Network error',
        "Can't reach the server. Check your connection and try again.",
      );
    }

    const problem: ProblemDetails | null = isProblemDetails(response.error) ? response.error : null;

    return new ApiError(
      response.status,
      problem?.title ?? response.statusText,
      problem?.detail ?? 'Something went wrong. Please try again.',
      problem?.errors ?? {},
    );
  }
}

// A type guard: `body is ProblemDetails` tells TypeScript that when this returns true, `body` has that type.
function isProblemDetails(body: unknown): body is ProblemDetails {
  return typeof body === 'object' && body !== null && 'title' in body;
}
