import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { API_URL } from './api.config';
import { AuthService } from './auth.service';
import { TEST_ROUTES } from './testing';
import { LoginResponse } from './models';

const HOUR = 3_600_000;

function session(overrides: Partial<LoginResponse> = {}): LoginResponse {
  return {
    token: 'jwt',
    expiresAt: new Date(Date.now() + HOUR).toISOString(),
    refreshToken: 'refresh',
    refreshTokenExpiresAt: new Date(Date.now() + 24 * HOUR).toISOString(),
    username: 'employee',
    fullName: 'Bank Employee',
    role: 'Employee',
    ...overrides,
  };
}

function setup(stored?: unknown): { auth: AuthService; backend: HttpTestingController } {
  if (stored !== undefined) {
    localStorage.setItem('banking.session', typeof stored === 'string' ? stored : JSON.stringify(stored));
  }
  TestBed.configureTestingModule({
    providers: [provideRouter(TEST_ROUTES), provideHttpClient(), provideHttpClientTesting()],
  });
  return { auth: TestBed.inject(AuthService), backend: TestBed.inject(HttpTestingController) };
}

describe('AuthService', () => {
  afterEach(() => localStorage.clear());

  it('stores the session after login', () => {
    const { auth, backend } = setup();

    auth.login({ username: 'employee', password: 'x' }).subscribe();
    backend.expectOne(`${API_URL}/auth/login`).flush(session());

    expect(auth.isLoggedIn()).toBe(true);
    expect(auth.isAdmin()).toBe(false);
    expect(JSON.parse(localStorage.getItem('banking.session')!).token).toBe('jwt');
  });

  it('keeps a stored session whose access token expired but refresh token is valid', () => {
    const { auth } = setup(session({ expiresAt: new Date(Date.now() - HOUR).toISOString() }));

    expect(auth.isLoggedIn()).toBe(true);
  });

  it('drops a stored session whose refresh token expired', () => {
    const { auth } = setup(session({ refreshTokenExpiresAt: new Date(Date.now() - 1000).toISOString() }));

    expect(auth.isLoggedIn()).toBe(false);
    expect(localStorage.getItem('banking.session')).toBeNull();
  });

  it('drops an old (v1.0) session without a refresh token', () => {
    const { token, expiresAt, username, fullName, role } = session();
    const { auth } = setup({ token, expiresAt, username, fullName, role });

    expect(auth.isLoggedIn()).toBe(false);
  });

  it('drops a corrupt stored session instead of crashing', () => {
    const { auth } = setup('{not json');

    expect(auth.isLoggedIn()).toBe(false);
  });

  it('revokes the refresh token on the server when signing out', () => {
    const { auth, backend } = setup(session({ refreshToken: 'to-revoke' }));

    auth.logout();

    const request = backend.expectOne(`${API_URL}/auth/logout`);
    expect(request.request.body).toEqual({ refreshToken: 'to-revoke' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(auth.isLoggedIn()).toBe(false);
  });

  it('signs out locally even if the API cannot be reached', () => {
    const { auth, backend } = setup(session());

    auth.logout();
    backend.expectOne(`${API_URL}/auth/logout`).error(new ProgressEvent('error'));

    expect(auth.isLoggedIn()).toBe(false);
  });
});
