// TypeScript shapes of the API's request and response DTOs (backend/Banking.Application/Dtos).

export type Role = 'Admin' | 'Employee';
export type AccountType = 'Checking' | 'Savings';
export type TransactionType = 'Deposit' | 'Withdrawal' | 'TransferIn' | 'TransferOut';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  username: string;
  fullName: string;
  role: Role;
}

export interface Customer {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  nationalId: string;
  accountCount: number;
  createdAt: string;
}

export interface CreateCustomerRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  nationalId: string;
}

export interface Account {
  id: number;
  accountNumber: string;
  type: AccountType;
  balance: number;
  customerId: number;
  customerName: string;
  createdAt: string;
}

export interface CreateAccountRequest {
  customerId: number;
  type: AccountType;
  initialDeposit: number;
}

export interface Transaction {
  id: number;
  accountId: number;
  accountNumber: string;
  type: TransactionType;
  amount: number;
  balanceAfter: number;
  description: string;
  referenceNumber: string;
  counterpartyAccountId: number | null;
  performedBy: string;
  createdAt: string;
}

export interface AmountRequest {
  amount: number;
  description?: string;
}

export interface TransferRequest {
  fromAccountId: number;
  toAccountId: number;
  amount: number;
  description?: string;
}

export interface TransferResponse {
  referenceNumber: string;
  amount: number;
  fromAccount: Account;
  toAccount: Account;
}

export interface DashboardSummary {
  totalCustomers: number;
  totalAccounts: number;
  totalBalance: number;
  transactionsToday: number;
  recentTransactions: Transaction[];
}

/** One page of a list endpoint (?page=&pageSize=). */
export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface User {
  id: number;
  username: string;
  fullName: string;
  role: Role;
  isActive: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  username: string;
  fullName: string;
  password: string;
  role: Role;
}
