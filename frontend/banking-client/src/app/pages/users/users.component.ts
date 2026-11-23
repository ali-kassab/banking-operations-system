import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { BankingApiService } from '../../core/banking-api.service';
import { errorMessage } from '../../core/error-message';
import { PagedResponse, Role, User } from '../../core/models';
import { PagerComponent } from '../../shared/pager.component';

/** Same rule as the API's PasswordRules: 8-100 characters with at least one letter and one digit. */
export const PASSWORD_PATTERN = /^(?=.*[A-Za-z])(?=.*\d).{8,100}$/;
export const USERNAME_PATTERN = /^[A-Za-z0-9._-]{3,50}$/;

/** Admin-only staff management: create users, activate/deactivate them and reset passwords. */
@Component({
  selector: 'app-users',
  imports: [ReactiveFormsModule, DatePipe, PagerComponent],
  templateUrl: './users.component.html',
})
export class UsersComponent {
  private readonly api = inject(BankingApiService);
  private readonly fb = inject(FormBuilder).nonNullable;
  protected readonly auth = inject(AuthService);

  protected readonly result = signal<PagedResponse<User> | null>(null);
  protected readonly page = signal(1);
  protected readonly showForm = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly success = signal('');

  /** The user whose password is being reset, or null. */
  protected readonly resetting = signal<User | null>(null);

  protected readonly form = this.fb.group({
    username: ['', [Validators.required, Validators.pattern(USERNAME_PATTERN)]],
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    password: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
    role: ['Employee' as Role, Validators.required],
  });

  protected readonly resetForm = this.fb.group({
    newPassword: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.api.getUsers(this.page()).subscribe({
      next: (data) => this.result.set(data),
      error: (err) => this.error.set(errorMessage(err)),
    });
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  toggleForm(): void {
    this.showForm.update((open) => !open);
    this.clearMessages();
  }

  isCurrentUser(user: User): boolean {
    return user.username === this.auth.currentUser()?.username;
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.clearMessages();

    this.api.createUser(this.form.getRawValue()).subscribe({
      next: (user) => {
        this.success.set(`User ${user.username} (${user.role}) was created.`);
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

  toggleActive(user: User): void {
    this.clearMessages();
    this.api.setUserActive(user.id, !user.isActive).subscribe({
      next: (updated) => {
        this.success.set(`User ${updated.username} is now ${updated.isActive ? 'active' : 'deactivated'}.`);
        this.load();
      },
      error: (err) => this.error.set(errorMessage(err)),
    });
  }

  startReset(user: User): void {
    this.clearMessages();
    this.resetForm.reset();
    this.resetting.set(user);
  }

  cancelReset(): void {
    this.resetting.set(null);
    this.clearMessages();
  }

  submitReset(): void {
    const user = this.resetting();
    if (!user || this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.clearMessages();

    this.api.resetUserPassword(user.id, this.resetForm.getRawValue().newPassword).subscribe({
      next: () => {
        this.success.set(`Password for ${user.username} was reset. Their open sessions were signed out.`);
        this.resetting.set(null);
        this.saving.set(false);
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.saving.set(false);
      },
    });
  }

  private clearMessages(): void {
    this.error.set('');
    this.success.set('');
  }
}
