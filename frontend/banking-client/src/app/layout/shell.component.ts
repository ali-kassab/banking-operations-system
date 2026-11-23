import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

/** Page frame for signed-in users: top navigation bar + the current page below it. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="topbar">
      <a class="brand" routerLink="/dashboard">🏦 Banking Ops</a>
      <nav>
        <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
        <a routerLink="/customers" routerLinkActive="active">Customers</a>
        <a routerLink="/accounts" routerLinkActive="active">Accounts</a>
        <a routerLink="/transfer" routerLinkActive="active">Transfer</a>
        <a routerLink="/transactions" routerLinkActive="active">Transactions</a>
        @if (auth.isAdmin()) {
          <a routerLink="/users" routerLinkActive="active">Users</a>
        }
      </nav>
      <div class="user">
        <span class="user-name">{{ auth.currentUser()?.fullName }}</span>
        <span class="badge" [title]="auth.currentUser()?.fullName ?? ''">{{ auth.currentUser()?.role }}</span>
        <button class="btn-link" (click)="auth.logout()">Sign out</button>
      </div>
    </header>
    <main class="container">
      <router-outlet />
    </main>
  `,
})
export class ShellComponent {
  protected readonly auth = inject(AuthService);
}
