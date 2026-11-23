import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { adminGuard, authGuard } from './auth.guard';
import { TEST_ROUTES } from './testing';
import { Role } from './models';

function signIn(role: Role): void {
  const later = new Date(Date.now() + 3_600_000).toISOString();
  localStorage.setItem(
    'banking.session',
    JSON.stringify({ token: 't', expiresAt: later, refreshToken: 'r', refreshTokenExpiresAt: later, username: 'u', fullName: 'U', role }),
  );
}

function run(guard: typeof authGuard): boolean | UrlTree {
  TestBed.configureTestingModule({ providers: [provideRouter(TEST_ROUTES), provideHttpClient()] });
  return TestBed.runInInjectionContext(() =>
    guard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
  ) as boolean | UrlTree;
}

describe('route guards', () => {
  afterEach(() => localStorage.clear());

  it('authGuard sends anonymous users to /login', () => {
    expect(run(authGuard).toString()).toBe('/login');
  });

  it('authGuard lets signed-in users through', () => {
    signIn('Employee');
    expect(run(authGuard)).toBe(true);
  });

  it('adminGuard sends employees to the dashboard', () => {
    signIn('Employee');
    expect(run(adminGuard).toString()).toBe('/dashboard');
  });

  it('adminGuard lets admins through', () => {
    signIn('Admin');
    expect(run(adminGuard)).toBe(true);
  });
});
