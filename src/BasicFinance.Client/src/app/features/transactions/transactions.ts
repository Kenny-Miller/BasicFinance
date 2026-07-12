import { Component, effect, inject, OnInit, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowDownAZ, lucideArrowUpAZ } from '@ng-icons/lucide';
import { BrnSelectImports } from '@spartan-ng/brain/select';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { Transaction } from '../../shared/api/transactions/transactions';
import { TransactionsListSkeleton } from '../../shared/ui/transactions-list-skeleton/transactions-list-skeleton';
import { TransactionCard } from './components/transaction-card/transaction-card';
import { TransactionsClient } from './data/transactions-client';

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
    HlmCardImports,
    HlmButtonImports,
    HlmSelectImports,
    HlmItemImports,
    BrnSelectImports,
    FormsModule,
    NgIcon,
    TransactionCard,
    TransactionsListSkeleton,
    HlmSeparatorImports,
    HlmSkeletonImports,
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

  readonly page = signal(1);
  readonly sortField = signal('Date');
  readonly sortDirection = signal('desc');
  readonly transactions = signal<Transaction[]>([]);
  readonly hasMore = signal(true);
  readonly sortOptions = SORT_OPTIONS;

  readonly resource = this.client.createResource(this.page, this.sortField, this.sortDirection);

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
