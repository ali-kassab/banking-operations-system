import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from './api.config';
import {
  Account,
  AmountRequest,
  CreateAccountRequest,
  CreateCustomerRequest,
  Customer,
  DashboardSummary,
  Transaction,
  TransferRequest,
  TransferResponse,
} from './models';

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
  getCustomers(search = ''): Observable<Customer[]> {
    const params = search ? new HttpParams().set('search', search) : undefined;
    return this.http.get<Customer[]>(`${API_URL}/customers`, { params });
  }

  getCustomer(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${API_URL}/customers/${id}`);
  }

  createCustomer(request: CreateCustomerRequest): Observable<Customer> {
    return this.http.post<Customer>(`${API_URL}/customers`, request);
  }

  // ----- Accounts -----
  getAccounts(customerId?: number): Observable<Account[]> {
    const params = customerId ? new HttpParams().set('customerId', customerId) : undefined;
    return this.http.get<Account[]>(`${API_URL}/accounts`, { params });
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

  getAccountTransactions(accountId: number): Observable<Transaction[]> {
    return this.http.get<Transaction[]>(`${API_URL}/accounts/${accountId}/transactions`);
  }

  // ----- Transfers & history -----
  transfer(request: TransferRequest): Observable<TransferResponse> {
    return this.http.post<TransferResponse>(`${API_URL}/transfers`, request);
  }

  getRecentTransactions(take = 50): Observable<Transaction[]> {
    return this.http.get<Transaction[]>(`${API_URL}/transactions`, {
      params: new HttpParams().set('take', take),
    });
  }
}
