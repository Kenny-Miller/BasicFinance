import { Component, effect, inject, OnInit, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { provideIcons } from '@ng-icons/core';
import { lucideArrowDownAZ, lucideArrowUpAZ } from '@ng-icons/lucide';
import { BrnSelectImports } from '@spartan-ng/brain/select';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { Transaction } from '../../shared/api/transactions/transactions';
import { TransactionsListSkeleton } from '../../shared/ui/transactions/transactions-list-skeleton/transactions-list-skeleton';
import { FilterBar } from './components/filter-bar/filter-bar';
import { TransactionCard } from './components/transaction-card/transaction-card';
import { TransactionFilters, TransactionsClient } from './data/transactions-client';

export interface SortOption {
  field: string;
  label: string;
}

const SORT_OPTIONS: SortOption[] = [
  { field: 'Date', label: 'Date' },
  { field: 'Amount', label: 'Amount' },
  { field: 'Description', label: 'Description' },
  { field: 'AccountName', label: 'Account' },
];

@Component({
  selector: 'app-transactions',
  imports: [
    HlmButtonImports,
    HlmSelectImports,
    HlmItemImports,
    BrnSelectImports,
    FormsModule,
    TransactionCard,
    TransactionsListSkeleton,
    HlmSeparatorImports,
    HlmSkeletonImports,
    FilterBar,
  ],
  templateUrl: './transactions.html',
  styleUrl: './transactions.css',
  providers: [
    provideIcons({
      lucideArrowDownAZ,
      lucideArrowUpAZ,
    }),
  ],
})
export class Transactions implements OnInit {
  private readonly client = inject(TransactionsClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly page = signal(1);
  readonly sortField = signal('Date');
  readonly sortDirection = signal('desc');
  readonly filters = signal<TransactionFilters>({
    startDate: '',
    endDate: '',
    minAmount: '',
    maxAmount: '',
    transactionTypeId: '',
    transactionCategoryId: '',
    search: '',
  });
  readonly transactions = signal<Transaction[]>([]);
  readonly hasMore = signal(true);
  readonly sortOptions = SORT_OPTIONS;

  readonly resource = this.client.createResource(
    this.page,
    this.sortField,
    this.sortDirection,
    this.filters,
  );

  private loadedPage = signal(0);
  sortFieldValue = 'Date';

  constructor() {
    effect(() => {
      const result = this.resource.value();
      if (!result || this.resource.isLoading()) {
        return;
      }

      if (result.page > this.loadedPage()) {
        const current = this.transactions();
        this.transactions.set([...current, ...result.items]);

        if (this.transactions().length >= result.totalCount) {
          this.hasMore.set(false);
        }

        untracked(() => this.loadedPage.set(result.page));
      }
    });
  }

  ngOnInit(): void {
    this.page.set(1);
    this.restoreFiltersFromRoute();
  }

  restoreFiltersFromRoute(): void {
    this.route.queryParams.subscribe((params) => {
      this.filters.set({
        startDate: params['startDate'] ?? '',
        endDate: params['endDate'] ?? '',
        minAmount: params['minAmount'] ?? '',
        maxAmount: params['maxAmount'] ?? '',
        transactionTypeId: params['transactionTypeId'] ?? '',
        transactionCategoryId: params['transactionCategoryId'] ?? '',
        search: params['search'] ?? '',
      });
    });
  }

  onFiltersChange(filters: TransactionFilters): void {
    this.filters.set(filters);
    this.page.set(1);
    this.transactions.set([]);
    this.hasMore.set(true);
    untracked(() => this.loadedPage.set(0));
    this.syncFiltersToRoute(filters);
  }

  syncFiltersToRoute(filters: TransactionFilters): void {
    const qp: Record<string, string | null> = {};
    if (filters.startDate) qp['startDate'] = filters.startDate;
    if (filters.endDate) qp['endDate'] = filters.endDate;
    if (filters.minAmount) qp['minAmount'] = filters.minAmount;
    if (filters.maxAmount) qp['maxAmount'] = filters.maxAmount;
    if (filters.transactionTypeId) qp['transactionTypeId'] = filters.transactionTypeId;
    if (filters.transactionCategoryId) qp['transactionCategoryId'] = filters.transactionCategoryId;
    if (filters.search) qp['search'] = filters.search;

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: qp,
      queryParamsHandling: 'merge',
    });
  }

  onSortChange(): void {
    this.page.set(1);
    this.sortField.set(this.sortFieldValue);
    this.transactions.set([]);
    this.hasMore.set(true);
    untracked(() => this.loadedPage.set(0));
  }

  toggleSortDirection(): void {
    this.page.set(1);
    this.sortDirection.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    this.transactions.set([]);
    this.hasMore.set(true);
    untracked(() => this.loadedPage.set(0));
  }

  loadMore(): void {
    this.page.update((p) => p + 1);
  }

  getSortIcon(): string {
    return this.sortDirection() === 'asc' ? 'lucideArrowUpAZ' : 'lucideArrowDownAZ';
  }
}
