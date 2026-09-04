import { Component, WritableSignal, computed, input, signal } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronLeft, lucideChevronRight, lucideCornerDownLeft } from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmSelectImports } from '@spartan-ng/helm/select';

const MAX_VISIBLE_PAGES = 5;

function buildPageWindow(total: number, current: number): (number | null)[] {
  if (total <= MAX_VISIBLE_PAGES) {
    return Array.from({ length: total }, (_, index) => index + 1);
  }

  const items: (number | null)[] = [1];
  if (current > 3) {
    items.push(null);
  }

  const windowStart = Math.max(2, current - 1);
  const windowEnd = Math.min(total - 1, current + 1);
  for (let page = windowStart; page <= windowEnd; page++) {
    items.push(page);
  }

  if (current < total - 2) {
    items.push(null);
  }
  items.push(total);

  return items;
}

@Component({
  selector: 'app-paginator',
  imports: [HlmButtonImports, HlmInputImports, HlmSelectImports, NgIcon],
  providers: [
    provideIcons({
      lucideChevronLeft,
      lucideChevronRight,
      lucideCornerDownLeft,
    }),
  ],
  templateUrl: './paginator.html',
  styleUrl: './paginator.css',
})
export class Paginator {
  readonly page = input.required<WritableSignal<number>>();
  readonly pageSize = input.required<WritableSignal<number>>();
  readonly totalCount = input.required<number>();

  readonly pageSizeOptions = [10, 20, 50, 100];
  readonly pageSelector = signal<number | null>(null);

  readonly currentPage = computed(() => this.page()());
  readonly currentPageSize = computed(() => this.pageSize()());

  readonly totalPages = computed(
    () => Math.max(1, Math.ceil(this.totalCount() / this.currentPageSize())),
  );
  readonly rangeStart = computed(() => {
    return this.totalCount() === 0 ? 0 : (this.currentPage() - 1) * this.currentPageSize() + 1;
  });
  readonly rangeEnd = computed(
    () => Math.min(this.currentPage() * this.currentPageSize(), this.totalCount()),
  );
  readonly previousDisabled = computed(() => this.currentPage() <= 1);
  readonly nextDisabled = computed(() => this.currentPage() >= this.totalPages());

  readonly pageWindow = computed(() => buildPageWindow(this.totalPages(), this.currentPage()));

  changePage(page: number): void {
    this.page().set(page);
  }

  selectPageSize(pageSize: number | null | undefined): void {
    if (pageSize == null) {
      return;
    }

    this.pageSize().set(pageSize);
    this.page().set(1);
  }

  selectPage(): void {
    const target = this.pageSelector();
    if (target == null) {
      return;
    }

    this.pageSelector.set(null);
    this.changePage(Math.min(Math.max(1, Math.trunc(target)), this.totalPages()));
  }

  pageSelectorKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.selectPage();
    }
  }

  pageSelectorChanged(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.pageSelector.set(target.valueAsNumber || null);
  }
}
