import { Component, computed, inject, OnInit } from '@angular/core';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { TransactionFilters } from '../../core/data-access/transaction-client';
import { PageService } from '../../core/page/page.service';
import { ThemeService } from '../../core/theme/theme.service';
import { TimePeriod } from '../../shared/data/time-period';
import { Paginator } from '../../shared/ui/paginator/paginator';
import { PeriodSelector } from '../../shared/ui/period-selector/period-selector';
import { TransactionsListSkeleton } from '../../shared/ui/transactions/transactions-list-skeleton/transactions-list-skeleton';
import { TransactionsList } from '../../shared/ui/transactions/transactions-list/transactions-list';
import { DailySpendChart } from './components/daily-spend-chart/daily-spend-chart';
import { FilterBar } from './components/filter-bar/filter-bar';
import { TransactionsSkeleton } from './components/transactions-skeleton/transactions-skeleton';
import { TransactionsSummaryTile } from './components/transactions-summary-tile/transactions-summary-tile';
import { TransactionsService } from './transactions-service';

const DELTA_LABELS: Record<TimePeriod, string> = {
  Weekly: 'last week',
  Monthly: 'last month',
  Quarterly: 'last quarter',
  Yearly: 'last year',
};

@Component({
  selector: 'app-transactions',
  imports: [
    HlmButtonImports,
    PeriodSelector,
    TransactionsSummaryTile,
    DailySpendChart,
    TransactionsList,
    TransactionsListSkeleton,
    FilterBar,
    Paginator,
    TransactionsSkeleton,
  ],
  templateUrl: './transactions.html',
  styleUrl: './transactions.css',
})
export class Transactions implements OnInit {
  private readonly transactionService = inject(TransactionsService);
  private readonly pageService = inject(PageService);
  private readonly themeService = inject(ThemeService);

  readonly appTheme = this.themeService.appTheme;
  readonly selectedPeriod = this.transactionService.period;
  readonly page = this.transactionService.page;
  readonly pageSize = this.transactionService.pageSize;
  readonly filters = this.transactionService.filters;
  readonly loading = this.transactionService.loading;
  readonly transactionsLoading = this.transactionService.transactionsLoading;
  readonly error = this.transactionService.error;
  readonly data = this.transactionService.data;
  readonly transactionTypes = this.transactionService.transactionTypes;
  readonly transactionCategories = this.transactionService.transactionCategories;

  readonly deltaLabel = computed(() => DELTA_LABELS[this.transactionService.period()]);

  readonly selectPeriod = (period: TimePeriod): void => this.transactionService.period.set(period);

  readonly refetchAll = (): void => this.transactionService.refetchAll();

  readonly applyFilters = (value: TransactionFilters): void => {
    this.transactionService.filters.set(value);
    this.transactionService.page.set(1);
  };

  ngOnInit(): void {
    this.pageService.setPageTitle('Transactions');
    this.pageService.setPageSubtitle('View your transactions');
  }
}
