import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { BankingApiService, MAX_PAGE_SIZE } from '../../core/banking-api.service';
import { errorMessage } from '../../core/error-message';
import { Account, AccountType, Customer, PagedResponse } from '../../core/models';
import { PagerComponent } from '../../shared/pager.component';

@Component({
  selector: 'app-accounts',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DatePipe, PagerComponent],
  templateUrl: './accounts.component.html',
})
export class AccountsComponent {
  private readonly api = inject(BankingApiService);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);

  /** Optional ?customerId= filter from the URL (bound by withComponentInputBinding). */
  readonly customerId = input<string>();

  protected readonly result = signal<PagedResponse<Account> | null>(null);
  protected readonly page = signal(1);
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
    // The customer picker shows the first MAX_PAGE_SIZE customers (sorted by name).
    this.api.getCustomers('', 1, MAX_PAGE_SIZE).subscribe((data) => this.customers.set(data.items));

    // A new ?customerId= filter starts again at page 1 and preselects that customer in the form.
    effect(() => {
      const id = Number(this.customerId()) || 0;
      untracked(() => {
        this.page.set(1);
        if (id) {
          this.form.controls.customerId.setValue(id);
        }
      });
    });

    // Reload whenever the filter or the page changes.
    effect(() => this.load(Number(this.customerId()) || undefined, this.page()));
  }

  private load(customerId: number | undefined, page: number): void {
    this.api.getAccounts(customerId, page).subscribe({
      next: (data) => this.result.set(data),
      error: (err) => this.error.set(errorMessage(err)),
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
        this.load(Number(this.customerId()) || undefined, this.page()); // the new account may land on any page
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.saving.set(false);
      },
    });
  }
}
