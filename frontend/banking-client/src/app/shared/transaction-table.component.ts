import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Transaction } from '../core/models';

/** Reusable table of transactions, used by the dashboard, account page and history page. */
@Component({
  selector: 'app-transaction-table',
  imports: [CurrencyPipe, DatePipe, RouterLink],
  template: `
    @if (transactions().length === 0) {
      <p class="muted">No transactions yet.</p>
    } @else {
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Date</th>
              @if (showAccount()) { <th>Account</th> }
              <th>Type</th>
              <th>Description</th>
              <th>Reference</th>
              <th class="num">Amount</th>
              <th class="num">Balance after</th>
              <th>By</th>
            </tr>
          </thead>
          <tbody>
            @for (t of transactions(); track t.id) {
              <tr>
                <td>{{ t.createdAt | date: 'short' }}</td>
                @if (showAccount()) {
                  <td><a [routerLink]="['/accounts', t.accountId]">{{ t.accountNumber }}</a></td>
                }
                <td><span [class]="'tag tag-' + t.type">{{ t.type }}</span></td>
                <td>{{ t.description }}</td>
                <td class="mono">{{ t.referenceNumber }}</td>
                <td class="num" [class.negative]="isDebit(t)">
                  {{ isDebit(t) ? '−' : '+' }}{{ t.amount | currency }}
                </td>
                <td class="num">{{ t.balanceAfter | currency }}</td>
                <td>{{ t.performedBy }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
})
export class TransactionTableComponent {
  readonly transactions = input.required<Transaction[]>();
  readonly showAccount = input(true);

  isDebit(t: Transaction): boolean {
    return t.type === 'Withdrawal' || t.type === 'TransferOut';
  }
}
