import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { map } from 'rxjs';
import { BankingApiService, MAX_PAGE_SIZE } from '../../core/banking-api.service';
import { errorMessage } from '../../core/error-message';
import { Account, TransferResponse } from '../../core/models';

@Component({
  selector: 'app-transfer',
  imports: [ReactiveFormsModule, CurrencyPipe],
  templateUrl: './transfer.component.html',
})
export class TransferComponent {
  private readonly api = inject(BankingApiService);

  /** Optional ?from= query parameter to preselect the source account. */
  readonly from = input<string>();

  protected readonly accounts = signal<Account[]>([]);
  protected readonly result = signal<TransferResponse | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal('');

  protected readonly form = inject(FormBuilder).nonNullable.group({
    fromAccountId: [0, Validators.min(1)],
    toAccountId: [0, Validators.min(1)],
    amount: [0, [Validators.required, Validators.min(0.01), Validators.max(1_000_000)]],
    description: ['', Validators.maxLength(200)],
  });

  // Turn the form value into a signal so the template can react to it (e.g. show the available balance).
  private readonly formValue = toSignal(this.form.valueChanges.pipe(map(() => this.form.getRawValue())), {
    initialValue: this.form.getRawValue(),
  });

  protected readonly sourceAccount = computed(() =>
    this.accounts().find((a) => a.id === Number(this.formValue().fromAccountId)),
  );

  /** Quick client-side check; the API is the real authority and rejects overdrafts too. */
  protected readonly exceedsBalance = computed(() => {
    const source = this.sourceAccount();
    return !!source && Number(this.formValue().amount) > source.balance;
  });

  constructor() {
    this.loadAccounts();
  }

  private loadAccounts(): void {
    // The account pickers list the first MAX_PAGE_SIZE accounts (sorted by account number).
    this.api.getAccounts(undefined, 1, MAX_PAGE_SIZE).subscribe({
      next: (data) => {
        this.accounts.set(data.items);
        const preselected = Number(this.from());
        if (preselected && this.form.controls.fromAccountId.value === 0) {
          this.form.controls.fromAccountId.setValue(preselected);
        }
      },
      error: (err) => this.error.set(errorMessage(err)),
    });
  }

  submit(): void {
    const value = this.form.getRawValue();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (value.fromAccountId === value.toAccountId) {
      this.error.set('Choose two different accounts.');
      return;
    }

    this.saving.set(true);
    this.error.set('');
    this.result.set(null);

    this.api
      .transfer({
        fromAccountId: Number(value.fromAccountId),
        toAccountId: Number(value.toAccountId),
        amount: value.amount,
        description: value.description || undefined,
      })
      .subscribe({
        next: (response) => {
          this.result.set(response);
          this.form.patchValue({ amount: 0, description: '' });
          this.form.markAsUntouched();
          this.saving.set(false);
          this.loadAccounts(); // refresh balances shown in the dropdowns
        },
        error: (err) => {
          this.error.set(errorMessage(err));
          this.saving.set(false);
        },
      });
  }
}
