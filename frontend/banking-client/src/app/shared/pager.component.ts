import { Component, input, output } from '@angular/core';
import { PagedResponse } from '../core/models';

/** "‹ Previous · Page 2 of 7 (134 items) · Next ›" under a paged table. Hidden when there is only one page. */
@Component({
  selector: 'app-pager',
  template: `
    @if (result(); as r) {
      @if (r.totalPages > 1) {
        <nav class="pager" aria-label="Pagination">
          <button class="btn btn-secondary" type="button" [disabled]="r.page <= 1" (click)="pageChange.emit(r.page - 1)">
            ‹ Previous
          </button>
          <span class="muted">Page {{ r.page }} of {{ r.totalPages }} ({{ r.totalCount }} items)</span>
          <button
            class="btn btn-secondary"
            type="button"
            [disabled]="r.page >= r.totalPages"
            (click)="pageChange.emit(r.page + 1)"
          >
            Next ›
          </button>
        </nav>
      }
    }
  `,
})
export class PagerComponent {
  readonly result = input<PagedResponse<unknown> | null>(null);
  readonly pageChange = output<number>();
}
