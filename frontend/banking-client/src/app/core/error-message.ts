import { HttpErrorResponse } from '@angular/common/http';

/**
 * Turns an API error into a readable sentence.
 * The API returns ProblemDetails: { title, detail } for business errors,
 * or { errors: { Field: [messages] } } for validation errors.
 */
export function errorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong.';
  }

  if (error.status === 0) {
    return 'Cannot reach the API. Is the backend running on http://localhost:5080?';
  }

  if (error.status === 403) {
    return 'You do not have permission to perform this action.';
  }

  const body = error.error;
  if (body?.errors) {
    return Object.values(body.errors as Record<string, string[]>).flat().join(' ');
  }

  return body?.detail ?? body?.title ?? `Request failed (${error.status}).`;
}
