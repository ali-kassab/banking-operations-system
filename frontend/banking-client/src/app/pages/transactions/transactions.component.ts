import { Component, inject, signal } from '@angular/core';
import { BankingApiService } from '../../core/banking-api.service';
import { errorMessage } from '../../core/error-message';
import { Transaction } from '../../core/models';
import { TransactionTableComponent } from '../../shared/transaction-table.component';

/** The latest transactions across all accounts. Per-account history lives on the account page. */
@Component({
  selector: 'app-transactions',
  imports: [TransactionTableComponent],
  template: `
    <div class="page-header">
      <h1>Transaction history</h1>
      <label class="inline">
        Show
        <select #take (change)="load(+take.value)">
          <option value="25">25</option>
          <option value="50" selected>50</option>
          <option value="100">100</option>
          <option value="200">200</option>
        </select>
      </label>
    </div>

    @if (error()) {
      <div class="alert alert-error">{{ error() }}</div>
    }

    <section class="card">
      <app-transaction-table [transactions]="transactions()" />
    </section>
  `,
})
export class TransactionsComponent {
  private readonly api = inject(BankingApiService);

  protected readonly transactions = signal<Transaction[]>([]);
  protected readonly error = signal('');

  constructor() {
    this.load(50);
  }

  load(take: number): void {
    this.api.getRecentTransactions(take).subscribe({
      next: (data) => this.transactions.set(data),
      error: (err) => this.error.set(errorMessage(err)),
    });
  }
}
