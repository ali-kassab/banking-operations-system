import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/auth.guard';
import { ShellComponent } from './layout/shell.component';
import { LoginComponent } from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { CustomersComponent } from './pages/customers/customers.component';
import { AccountsComponent } from './pages/accounts/accounts.component';
import { AccountDetailComponent } from './pages/account-detail/account-detail.component';
import { TransferComponent } from './pages/transfer/transfer.component';
import { TransactionsComponent } from './pages/transactions/transactions.component';
import { UsersComponent } from './pages/users/users.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent, title: 'Sign in' },
  {
    // Every page inside the shell (top navigation) requires a signed-in user.
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: DashboardComponent, title: 'Dashboard' },
      { path: 'customers', component: CustomersComponent, title: 'Customers' },
      { path: 'accounts', component: AccountsComponent, title: 'Accounts' },
      { path: 'accounts/:id', component: AccountDetailComponent, title: 'Account' },
      { path: 'transfer', component: TransferComponent, title: 'Transfer' },
      { path: 'transactions', component: TransactionsComponent, title: 'Transactions' },
      { path: 'users', component: UsersComponent, title: 'Users', canActivate: [adminGuard] },
    ],
  },
  { path: '**', redirectTo: '' },
];
