import { Component, computed, inject, OnInit } from '@angular/core';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { PageService } from '../../core/page/page.service';
import { ThemeService } from '../../core/theme/theme.service';
import { TimePeriod } from '../../shared/data/time-period';
import { Pagination } from '../../shared/ui/pagination/pagination';
import { PeriodSelector } from '../../shared/ui/period-selector/period-selector';
import { TransactionsList } from '../../shared/ui/transactions/transactions-list/transactions-list';
import { DailySpendChart } from './components/daily-spend-chart/daily-spend-chart';
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
    Pagination,
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
  readonly loading = this.transactionService.loading;
  readonly error = this.transactionService.error;
  readonly data = this.transactionService.data;

  readonly deltaLabel = computed(() => DELTA_LABELS[this.transactionService.period()]);

  readonly selectPeriod = (period: TimePeriod): void => this.transactionService.period.set(period);

  readonly refetchAll = (): void => this.transactionService.refetchAll();

  readonly setPage = (value: number): void => this.transactionService.page.set(value);

  readonly setPageSize = (value: number): void => {
    this.transactionService.pageSize.set(value);
    this.transactionService.page.set(1);
  };

  ngOnInit(): void {
    this.pageService.setPageTitle('Transactions');
    this.pageService.setPageSubtitle('View your transactions');
  }
}
