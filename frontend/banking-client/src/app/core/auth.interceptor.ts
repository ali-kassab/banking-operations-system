import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_URL } from './api.config';
import { AuthService } from './auth.service';

function withToken(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
}

/**
 * Adds "Authorization: Bearer <token>" to every API call.
 * When the API answers 401 (usually an expired access token), it refreshes the session once
 * and repeats the request. If the refresh fails too, the user is signed out.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);

  // Login/refresh/logout carry their own credentials and must never trigger a refresh themselves.
  if (request.url.startsWith(`${API_URL}/auth/`)) {
    return next(request);
  }

  const token = auth.token;
  return next(withToken(request, token)).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || !token) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        catchError((refreshError) => {
          auth.clear();
          return throwError(() => refreshError);
        }),
        switchMap((session) => next(withToken(request, session.token))),
      );
    }),
  );
};
