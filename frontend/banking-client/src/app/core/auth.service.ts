import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { API_URL } from './api.config';
import { LoginRequest, LoginResponse } from './models';

const STORAGE_KEY = 'banking.session';

/**
 * Signs the user in and keeps the current session (token + user info).
 * The session is stored in localStorage so a page refresh keeps you logged in.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<LoginResponse | null>(this.loadSession());

  readonly currentUser = this.session.asReadonly();
  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly isAdmin = computed(() => this.session()?.role === 'Admin');

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${API_URL}/auth/login`, request).pipe(
      tap((response) => {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(response));
        this.session.set(response);
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
    this.router.navigate(['/login']);
  }

  get token(): string | null {
    return this.session()?.token ?? null;
  }

  private loadSession(): LoginResponse | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }

    const saved = JSON.parse(raw) as LoginResponse;
    // Drop expired tokens instead of sending requests that will fail with 401.
    if (new Date(saved.expiresAt) <= new Date()) {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return saved;
  }
}
