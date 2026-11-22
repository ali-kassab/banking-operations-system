import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { BankingApiService } from '../../core/banking-api.service';
import { errorMessage } from '../../core/error-message';
import { Account, AccountType, Customer } from '../../core/models';

@Component({
  selector: 'app-accounts',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './accounts.component.html',
})
export class AccountsComponent {
  private readonly api = inject(BankingApiService);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);

  /** Optional ?customerId= filter from the URL (bound by withComponentInputBinding). */
  readonly customerId = input<string>();

  protected readonly accounts = signal<Account[]>([]);
  protected readonly customers = signal<Customer[]>([]);
  protected readonly showForm = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly success = signal('');

  protected readonly form = inject(FormBuilder).nonNullable.group({
    customerId: [0, [Validators.required, Validators.min(1)]],
    type: ['Checking' as AccountType, Validators.required],
    initialDeposit: [0, [Validators.min(0), Validators.max(1_000_000)]],
  });

  constructor() {
    this.api.getCustomers().subscribe((data) => this.customers.set(data));

    // Reload whenever the ?customerId= filter changes.
    effect(() => {
      const id = Number(this.customerId()) || undefined;
      this.api.getAccounts(id).subscribe({
        next: (data) => this.accounts.set(data),
        error: (err) => this.error.set(errorMessage(err)),
      });
      if (id) {
        this.form.controls.customerId.setValue(id);
      }
    });
  }

  filterByCustomer(value: string): void {
    this.router.navigate(['/accounts'], { queryParams: { customerId: value || null } });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set('');

    const value = this.form.getRawValue();
    this.api.createAccount({ ...value, customerId: Number(value.customerId) }).subscribe({
      next: (account) => {
        this.success.set(`Account ${account.accountNumber} opened for ${account.customerName}.`);
        this.showForm.set(false);
        this.saving.set(false);
        this.accounts.update((list) => [...list, account]);
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.saving.set(false);
      },
    });
  }
}
