import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PagedResponse } from '../core/models';
import { PagerComponent } from './pager.component';

function page(current: number, totalPages: number): PagedResponse<unknown> {
  return { items: [], page: current, pageSize: 10, totalCount: totalPages * 10, totalPages };
}

describe('PagerComponent', () => {
  let fixture: ComponentFixture<PagerComponent>;
  let emitted: number[];

  async function render(result: PagedResponse<unknown> | null): Promise<HTMLElement> {
    fixture.componentRef.setInput('result', result);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    fixture = TestBed.createComponent(PagerComponent);
    emitted = [];
    fixture.componentInstance.pageChange.subscribe((p) => emitted.push(p));
  });

  it('renders nothing for a single page', async () => {
    expect((await render(page(1, 1))).querySelector('nav')).toBeNull();
    expect((await render(null)).querySelector('nav')).toBeNull();
  });

  it('shows the position and total', async () => {
    const el = await render(page(2, 5));
    expect(el.textContent).toContain('Page 2 of 5 (50 items)');
  });

  it('disables Previous on the first page and Next on the last', async () => {
    let [prev, next] = Array.from((await render(page(1, 3))).querySelectorAll('button'));
    expect(prev.disabled).toBe(true);
    expect(next.disabled).toBe(false);

    [prev, next] = Array.from((await render(page(3, 3))).querySelectorAll('button'));
    expect(prev.disabled).toBe(false);
    expect(next.disabled).toBe(true);
  });

  it('emits the neighbouring page numbers', async () => {
    const [prev, next] = Array.from((await render(page(2, 3))).querySelectorAll('button'));
    prev.click();
    next.click();
    expect(emitted).toEqual([1, 3]);
  });
});
