import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, shareReplay, tap } from 'rxjs';
import { API_URL } from './api.config';
import { LoginRequest, LoginResponse } from './models';

const STORAGE_KEY = 'banking.session';

/**
 * Signs the user in and keeps the current session (tokens + user info).
 * The session is stored in localStorage so a page refresh keeps you logged in.
 *
 * The access token (JWT) is short-lived. When it expires, the interceptor calls refresh(),
 * which trades the single-use refresh token for a new pair without asking for the password again.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<LoginResponse | null>(this.loadSession());

  /** The refresh call in progress, shared so parallel 401s trigger only one refresh. */
  private refreshInFlight: Observable<LoginResponse> | null = null;

  readonly currentUser = this.session.asReadonly();
  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly isAdmin = computed(() => this.session()?.role === 'Admin');

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${API_URL}/auth/login`, request).pipe(tap((response) => this.store(response)));
  }

  /** Gets a new token pair. Fails (401) when the refresh token is expired, revoked or reused. */
  refresh(): Observable<LoginResponse> {
    const refreshToken = this.session()?.refreshToken;
    if (!this.refreshInFlight) {
      this.refreshInFlight = this.http
        .post<LoginResponse>(`${API_URL}/auth/refresh`, { refreshToken })
        .pipe(
          tap((response) => this.store(response)),
          finalize(() => (this.refreshInFlight = null)),
          shareReplay(1),
        );
    }
    return this.refreshInFlight;
  }

  /** Ends the session on the server (revokes the refresh token) and locally. */
  logout(): void {
    const refreshToken = this.session()?.refreshToken;
    if (refreshToken) {
      // Fire and forget: the local sign-out must not depend on the API being reachable.
      this.http.post(`${API_URL}/auth/logout`, { refreshToken }).subscribe({ error: () => undefined });
    }
    this.clear();
  }

  /** Drops the local session without calling the API (used when the server already rejected it). */
  clear(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
    this.router.navigate(['/login']);
  }

  get token(): string | null {
    return this.session()?.token ?? null;
  }

  private store(response: LoginResponse): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(response));
    this.session.set(response);
  }

  private loadSession(): LoginResponse | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }

    let saved: LoginResponse;
    try {
      saved = JSON.parse(raw) as LoginResponse;
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }

    // An expired access token is fine (it gets refreshed); an expired or missing refresh token is not.
    if (!saved.refreshToken || new Date(saved.refreshTokenExpiresAt) <= new Date()) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return saved;
  }
}
