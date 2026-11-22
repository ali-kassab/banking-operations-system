import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BankingApiService } from '../../core/banking-api.service';
import { errorMessage } from '../../core/error-message';
import { Customer } from '../../core/models';

@Component({
  selector: 'app-customers',
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './customers.component.html',
})
export class CustomersComponent {
  private readonly api = inject(BankingApiService);

  protected readonly customers = signal<Customer[]>([]);
  protected readonly search = signal('');
  protected readonly showForm = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly success = signal('');

  // Validators mirror the API's rules so users get instant feedback;
  // the API still validates everything itself.
  protected readonly form = inject(FormBuilder).nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(20)]],
    nationalId: ['', [Validators.required, Validators.maxLength(20)]],
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.api.getCustomers(this.search()).subscribe({
      next: (data) => this.customers.set(data),
      error: (err) => this.error.set(errorMessage(err)),
    });
  }

  onSearch(value: string): void {
    this.search.set(value);
    this.load();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set('');

    this.api.createCustomer(this.form.getRawValue()).subscribe({
      next: (customer) => {
        this.success.set(`Customer ${customer.fullName} was created.`);
        this.form.reset();
        this.showForm.set(false);
        this.saving.set(false);
        this.load();
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.saving.set(false);
      },
    });
  }
}
