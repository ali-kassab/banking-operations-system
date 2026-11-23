import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from './api.config';
import { BankingApiService, PAGE_SIZE } from './banking-api.service';

describe('BankingApiService', () => {
  let api: BankingApiService;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(BankingApiService);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('requests the first page with the default page size', () => {
    api.getTransactions().subscribe();

    const request = backend.expectOne((r) => r.url === `${API_URL}/transactions`);
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('pageSize')).toBe(String(PAGE_SIZE));
    request.flush({ items: [], page: 1, pageSize: PAGE_SIZE, totalCount: 0, totalPages: 0 });
  });

  it('sends search, page and page size for customers', () => {
    api.getCustomers('khalil', 3, 10).subscribe();

    const request = backend.expectOne((r) => r.url === `${API_URL}/customers`);
    expect(request.request.params.get('search')).toBe('khalil');
    expect(request.request.params.get('page')).toBe('3');
    expect(request.request.params.get('pageSize')).toBe('10');
    request.flush({});
  });

  it('omits empty filters', () => {
    api.getAccounts(undefined, 2).subscribe();

    const request = backend.expectOne((r) => r.url === `${API_URL}/accounts`);
    expect(request.request.params.has('customerId')).toBe(false);
    expect(request.request.params.get('page')).toBe('2');
    request.flush({});
  });

  it('uses PUT for user status and password changes', () => {
    api.setUserActive(7, false).subscribe();
    api.resetUserPassword(7, 'NewPass123').subscribe();

    const status = backend.expectOne(`${API_URL}/users/7/status`);
    expect(status.request.method).toBe('PUT');
    expect(status.request.body).toEqual({ isActive: false });
    status.flush({});

    const password = backend.expectOne(`${API_URL}/users/7/password`);
    expect(password.request.method).toBe('PUT');
    expect(password.request.body).toEqual({ newPassword: 'NewPass123' });
    password.flush(null);
  });
});
