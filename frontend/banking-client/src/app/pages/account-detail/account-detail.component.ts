import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { BankingApiService } from '../../core/banking-api.service';
import { errorMessage } from '../../core/error-message';
import { Account, Transaction } from '../../core/models';
import { TransactionTableComponent } from '../../shared/transaction-table.component';

/** One account: its details, a deposit/withdraw form and its full transaction history. */
@Component({
  selector: 'app-account-detail',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DatePipe, TransactionTableComponent],
  templateUrl: './account-detail.component.html',
})
export class AccountDetailComponent {
  private readonly api = inject(BankingApiService);

  /** The :id route parameter. */
  readonly id = input.required<string>();

  protected readonly account = signal<Account | null>(null);
  protected readonly transactions = signal<Transaction[]>([]);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly success = signal('');

  protected readonly form = inject(FormBuilder).nonNullable.group({
    operation: ['deposit' as 'deposit' | 'withdraw'],
    amount: [0, [Validators.required, Validators.min(0.01), Validators.max(1_000_000)]],
    description: ['', Validators.maxLength(200)],
  });

  constructor() {
    effect(() => this.load(Number(this.id())));
  }

  private load(accountId: number): void {
    this.api.getAccount(accountId).subscribe({
      next: (data) => this.account.set(data),
      error: (err) => this.error.set(errorMessage(err)),
    });
    this.api.getAccountTransactions(accountId).subscribe((data) => this.transactions.set(data));
  }

  submit(): void {
    const account = this.account();
    if (!account || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { operation, amount, description } = this.form.getRawValue();
    const request = { amount, description: description || undefined };
    const call: Observable<Transaction> =
      operation === 'deposit' ? this.api.deposit(account.id, request) : this.api.withdraw(account.id, request);

    this.saving.set(true);
    this.error.set('');
    this.success.set('');

    call.subscribe({
      next: (t) => {
        this.success.set(`${t.type} of ${amount.toFixed(2)} completed. Reference ${t.referenceNumber}.`);
        this.form.patchValue({ amount: 0, description: '' });
        this.form.markAsUntouched();
        this.saving.set(false);
        this.load(account.id); // refresh balance and history from the server
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.saving.set(false);
      },
    });
  }
}
