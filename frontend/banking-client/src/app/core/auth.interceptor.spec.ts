import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { API_URL } from './api.config';
import { authInterceptor } from './auth.interceptor';
import { TEST_ROUTES } from './testing';
import { AuthService } from './auth.service';
import { LoginResponse } from './models';

function session(token: string, refreshToken: string): LoginResponse {
  const inOneHour = new Date(Date.now() + 3_600_000).toISOString();
  return {
    token,
    expiresAt: inOneHour,
    refreshToken,
    refreshTokenExpiresAt: inOneHour,
    username: 'admin',
    fullName: 'Admin',
    role: 'Admin',
  };
}

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    localStorage.setItem('banking.session', JSON.stringify(session('old-token', 'refresh-1')));
    TestBed.configureTestingModule({
      providers: [
        provideRouter(TEST_ROUTES),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => {
    backend.verify();
    localStorage.clear();
  });

  it('adds the bearer token to API requests', () => {
    http.get(`${API_URL}/customers`).subscribe();

    const request = backend.expectOne(`${API_URL}/customers`);
    expect(request.request.headers.get('Authorization')).toBe('Bearer old-token');
    request.flush({});
  });

  it('does not touch auth endpoints', () => {
    http.post(`${API_URL}/auth/login`, {}).subscribe({ error: () => undefined });

    const request = backend.expectOne(`${API_URL}/auth/login`);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({}, { status: 401, statusText: 'Unauthorized' });
    // A 401 from login must not start a refresh.
    backend.expectNone(`${API_URL}/auth/refresh`);
  });

  it('refreshes once on 401 and retries the request with the new token', () => {
    let body: unknown;
    http.get(`${API_URL}/customers`).subscribe((b) => (body = b));

    backend.expectOne(`${API_URL}/customers`).flush(null, { status: 401, statusText: 'Unauthorized' });

    const refresh = backend.expectOne(`${API_URL}/auth/refresh`);
    expect(refresh.request.body).toEqual({ refreshToken: 'refresh-1' });
    refresh.flush(session('new-token', 'refresh-2'));

    const retry = backend.expectOne(`${API_URL}/customers`);
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-token');
    retry.flush({ ok: true });

    expect(body).toEqual({ ok: true });
    expect(auth.token).toBe('new-token');
    expect(JSON.parse(localStorage.getItem('banking.session')!).refreshToken).toBe('refresh-2');
  });

  it('shares one refresh between parallel 401 responses', () => {
    http.get(`${API_URL}/customers`).subscribe();
    http.get(`${API_URL}/accounts`).subscribe();

    backend.expectOne(`${API_URL}/customers`).flush(null, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne(`${API_URL}/accounts`).flush(null, { status: 401, statusText: 'Unauthorized' });

    // Only ONE refresh call, even though two requests failed.
    backend.expectOne(`${API_URL}/auth/refresh`).flush(session('new-token', 'refresh-2'));

    backend.expectOne(`${API_URL}/customers`).flush({});
    backend.expectOne(`${API_URL}/accounts`).flush({});
  });

  it('signs out when the refresh fails', () => {
    let failed = false;
    http.get(`${API_URL}/customers`).subscribe({ error: () => (failed = true) });

    backend.expectOne(`${API_URL}/customers`).flush(null, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne(`${API_URL}/auth/refresh`).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(failed).toBe(true);
    expect(auth.isLoggedIn()).toBe(false);
    expect(localStorage.getItem('banking.session')).toBeNull();
  });

  it('passes other errors through without refreshing', () => {
    let status = 0;
    http.get(`${API_URL}/accounts`).subscribe({ error: (e) => (status = e.status) });

    backend.expectOne(`${API_URL}/accounts`).flush(null, { status: 403, statusText: 'Forbidden' });

    expect(status).toBe(403);
    backend.expectNone(`${API_URL}/auth/refresh`);
    expect(auth.isLoggedIn()).toBe(true);
  });
});
