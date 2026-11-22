import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BankingApiService } from '../../core/banking-api.service';
import { errorMessage } from '../../core/error-message';
import { DashboardSummary } from '../../core/models';
import { TransactionTableComponent } from '../../shared/transaction-table.component';

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, RouterLink, TransactionTableComponent],
  template: `
    <h1>Dashboard</h1>

    @if (error()) {
      <div class="alert alert-error">{{ error() }}</div>
    }

    @if (summary(); as s) {
      <div class="stats">
        <div class="card stat"><span>Customers</span><strong>{{ s.totalCustomers }}</strong></div>
        <div class="card stat"><span>Accounts</span><strong>{{ s.totalAccounts }}</strong></div>
        <div class="card stat"><span>Total deposits held</span><strong>{{ s.totalBalance | currency }}</strong></div>
        <div class="card stat"><span>Transactions today</span><strong>{{ s.transactionsToday }}</strong></div>
      </div>

      <section class="card">
        <div class="section-header">
          <h2>Recent transactions</h2>
          <a routerLink="/transactions">View all →</a>
        </div>
        <app-transaction-table [transactions]="s.recentTransactions" />
      </section>
    } @else if (!error()) {
      <p class="muted">Loading…</p>
    }
  `,
})
export class DashboardComponent {
  private readonly api = inject(BankingApiService);

  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly error = signal('');

  constructor() {
    this.api.getDashboard().subscribe({
      next: (data) => this.summary.set(data),
      error: (err) => this.error.set(errorMessage(err)),
    });
  }
}
