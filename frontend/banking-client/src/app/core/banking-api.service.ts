import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from './api.config';
import {
  Account,
  AmountRequest,
  CreateAccountRequest,
  CreateCustomerRequest,
  CreateUserRequest,
  Customer,
  DashboardSummary,
  PagedResponse,
  Transaction,
  TransferRequest,
  TransferResponse,
  User,
} from './models';

/** Default page size of the list pages. The API allows up to MAX_PAGE_SIZE. */
export const PAGE_SIZE = 20;

/** Largest page the API returns; used to fill dropdowns (customer and account pickers). */
export const MAX_PAGE_SIZE = 100;

function pageParams(page: number, pageSize: number): HttpParams {
  return new HttpParams().set('page', page).set('pageSize', pageSize);
}

/**
 * One thin wrapper per API endpoint. Components call these methods
 * instead of building URLs themselves.
 */
@Injectable({ providedIn: 'root' })
export class BankingApiService {
  private readonly http = inject(HttpClient);

  // ----- Dashboard -----
  getDashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${API_URL}/dashboard/summary`);
  }

  // ----- Customers -----
  getCustomers(search = '', page = 1, pageSize = PAGE_SIZE): Observable<PagedResponse<Customer>> {
    let params = pageParams(page, pageSize);
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<PagedResponse<Customer>>(`${API_URL}/customers`, { params });
  }

  getCustomer(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${API_URL}/customers/${id}`);
  }

  createCustomer(request: CreateCustomerRequest): Observable<Customer> {
    return this.http.post<Customer>(`${API_URL}/customers`, request);
  }

  // ----- Accounts -----
  getAccounts(customerId?: number, page = 1, pageSize = PAGE_SIZE): Observable<PagedResponse<Account>> {
    let params = pageParams(page, pageSize);
    if (customerId) {
      params = params.set('customerId', customerId);
    }
    return this.http.get<PagedResponse<Account>>(`${API_URL}/accounts`, { params });
  }

  getAccount(id: number): Observable<Account> {
    return this.http.get<Account>(`${API_URL}/accounts/${id}`);
  }

  createAccount(request: CreateAccountRequest): Observable<Account> {
    return this.http.post<Account>(`${API_URL}/accounts`, request);
  }

  deposit(accountId: number, request: AmountRequest): Observable<Transaction> {
    return this.http.post<Transaction>(`${API_URL}/accounts/${accountId}/deposit`, request);
  }

  withdraw(accountId: number, request: AmountRequest): Observable<Transaction> {
    return this.http.post<Transaction>(`${API_URL}/accounts/${accountId}/withdraw`, request);
  }

  getAccountTransactions(accountId: number, page = 1, pageSize = PAGE_SIZE): Observable<PagedResponse<Transaction>> {
    return this.http.get<PagedResponse<Transaction>>(`${API_URL}/accounts/${accountId}/transactions`, {
      params: pageParams(page, pageSize),
    });
  }

  // ----- Transfers & history -----
  transfer(request: TransferRequest): Observable<TransferResponse> {
    return this.http.post<TransferResponse>(`${API_URL}/transfers`, request);
  }

  getTransactions(page = 1, pageSize = PAGE_SIZE): Observable<PagedResponse<Transaction>> {
    return this.http.get<PagedResponse<Transaction>>(`${API_URL}/transactions`, {
      params: pageParams(page, pageSize),
    });
  }

  // ----- Users (Admin only) -----
  getUsers(page = 1, pageSize = PAGE_SIZE): Observable<PagedResponse<User>> {
    return this.http.get<PagedResponse<User>>(`${API_URL}/users`, { params: pageParams(page, pageSize) });
  }

  createUser(request: CreateUserRequest): Observable<User> {
    return this.http.post<User>(`${API_URL}/users`, request);
  }

  setUserActive(id: number, isActive: boolean): Observable<User> {
    return this.http.put<User>(`${API_URL}/users/${id}/status`, { isActive });
  }

  resetUserPassword(id: number, newPassword: string): Observable<void> {
    return this.http.put<void>(`${API_URL}/users/${id}/password`, { newPassword });
  }
}
