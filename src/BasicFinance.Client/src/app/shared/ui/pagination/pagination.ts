import { Component, computed, input, output, signal } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideChevronLeft,
  lucideChevronRight,
  lucideCornerDownLeft,
} from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmSelectImports } from '@spartan-ng/helm/select';

@Component({
  selector: 'app-pagination',
  imports: [HlmButtonImports, HlmInputImports, HlmSelectImports, NgIcon],
  providers: [
    provideIcons({
      lucideChevronLeft,
      lucideChevronRight,
      lucideCornerDownLeft,
    }),
  ],
  templateUrl: './pagination.html',
  styleUrl: './pagination.css',
})
export class Pagination {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  readonly pageSizeOptions = [10, 20, 50, 100];
  readonly jumpTo = signal<number | null>(null);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
  readonly rangeStart = computed(() => {
    if (this.totalCount() === 0) {
      return 0;
    }
    return (this.page() - 1) * this.pageSize() + 1;
  });
  readonly rangeEnd = computed(() => Math.min(this.page() * this.pageSize(), this.totalCount()));
  readonly previousDisabled = computed(() => this.page() <= 1);
  readonly nextDisabled = computed(() => this.page() >= this.totalPages());

  readonly pageItems = computed<(number | null)[]>(() => {
    const total = this.totalPages();
    const current = this.page();
    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
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
  });

  goToPage(page: number): void {
    this.pageChange.emit(page);
  }

  selectPageSize(pageSize: number | null | undefined): void {
    if (pageSize != null) {
      this.pageSizeChange.emit(pageSize);
    }
  }

  jumpPage(): void {
    const target = this.jumpTo();
    if (target == null) {
      return;
    }

    this.jumpTo.set(null);
    this.goToPage(Math.min(Math.max(1, Math.trunc(target)), this.totalPages()));
  }

  onJumpKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.jumpPage();
    }
  }

  jumpInputChanged(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.jumpTo.set(target.valueAsNumber || null);
  }
}
