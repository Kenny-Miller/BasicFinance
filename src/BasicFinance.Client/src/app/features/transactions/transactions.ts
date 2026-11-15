import { Component, computed, inject } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';

import { TransactionFilters } from '../../core/data-access/transaction-client';
import { ThemeService } from '../../core/theme/theme.service';
import { TIME_PERIOD_CONFIG } from '../../shared/data/time-period';
import { AppPageHeader } from '../../shared/ui/app-page-header/app-page-header';
import { Paginator } from '../../shared/ui/paginator/paginator';
import { TimePeriodSelector } from '../../shared/ui/time-period-selector/time-period-selector';
import { TransactionsListSkeleton } from '../../shared/ui/transactions/transactions-list-skeleton/transactions-list-skeleton';
import { TransactionsList } from '../../shared/ui/transactions/transactions-list/transactions-list';
import { DailySpendChart } from './components/daily-spend-chart/daily-spend-chart';
import { FilterBar } from './components/filter-bar/filter-bar';
import { TransactionsSkeleton } from './components/transactions-skeleton/transactions-skeleton';
import { TransactionsSummaryTile } from './components/transactions-summary-tile/transactions-summary-tile';
import { TransactionsService } from './transactions-service';

@Component({
  selector: 'app-transactions',
  imports: [
    HlmButtonImports,
    TimePeriodSelector,
    TransactionsSummaryTile,
    DailySpendChart,
    TransactionsList,
    TransactionsListSkeleton,
    FilterBar,
    Paginator,
    TransactionsSkeleton,
    AppPageHeader,
  ],
  templateUrl: './transactions.html',
  styleUrl: './transactions.css',
})
export class Transactions {
  private readonly transactionService = inject(TransactionsService);
  private readonly themeService = inject(ThemeService);

  readonly appTheme = this.themeService.appTheme;
  readonly selectedTimePeriod = this.transactionService.selectedTimePeriod;
  readonly selectedDate = this.transactionService.selectedDate;
  readonly page = this.transactionService.page;
  readonly pageSize = this.transactionService.pageSize;
  readonly filters = this.transactionService.filters;
  readonly loading = this.transactionService.loading;
  readonly transactionsLoading = this.transactionService.transactionsLoading;
  readonly error = this.transactionService.error;
  readonly data = this.transactionService.data;
  readonly transactionTypes = this.transactionService.transactionTypes;
  readonly transactionCategories = this.transactionService.transactionCategories;

  readonly deltaLabel = computed(() => TIME_PERIOD_CONFIG[this.selectedTimePeriod()].previousLabel);

  readonly refetchAll = (): void => this.transactionService.refetchAll();

  readonly applyFilters = (value: TransactionFilters): void => {
    this.transactionService.filters.set(value);
    this.transactionService.page.set(1);
  };
}
