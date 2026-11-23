import { Component, inject, signal } from '@angular/core';
import { BankingApiService, PAGE_SIZE } from '../../core/banking-api.service';
import { errorMessage } from '../../core/error-message';
import { PagedResponse, Transaction } from '../../core/models';
import { PagerComponent } from '../../shared/pager.component';
import { TransactionTableComponent } from '../../shared/transaction-table.component';

/** All transactions across all accounts, newest first. Per-account history lives on the account page. */
@Component({
  selector: 'app-transactions',
  imports: [TransactionTableComponent, PagerComponent],
  template: `
    <div class="page-header">
      <h1>Transaction history</h1>
      <label class="inline">
        Per page
        <select #size (change)="changePageSize(+size.value)">
          @for (option of pageSizes; track option) {
            <option [value]="option" [selected]="option === pageSize()">{{ option }}</option>
          }
        </select>
      </label>
    </div>

    @if (error()) {
      <div class="alert alert-error">{{ error() }}</div>
    }

    <section class="card">
      <app-transaction-table [transactions]="result()?.items ?? []" />
      <app-pager [result]="result()" (pageChange)="load($event)" />
    </section>
  `,
})
export class TransactionsComponent {
  private readonly api = inject(BankingApiService);

  protected readonly pageSizes = [10, 20, 50, 100];
  protected readonly pageSize = signal(PAGE_SIZE);
  protected readonly result = signal<PagedResponse<Transaction> | null>(null);
  protected readonly error = signal('');

  constructor() {
    this.load(1);
  }

  load(page: number): void {
    this.api.getTransactions(page, this.pageSize()).subscribe({
      next: (data) => this.result.set(data),
      error: (err) => this.error.set(errorMessage(err)),
    });
  }

  changePageSize(size: number): void {
    this.pageSize.set(size);
    this.load(1);
  }
}
