import { Component, inject, OnInit } from '@angular/core';
import { PageService } from '../../core/page/page.service';
import { TimePeriod } from '../../shared/data/time-period';
import { PeriodSelector } from '../../shared/ui/period-selector/period-selector';
import { CategoryBreakdownList } from './components/category-breakdown-list/category-breakdown-list';
import { SpendingSkeleton } from './components/spending-skeleton/spending-skeleton';
import { SpendingSummaryTile } from './components/spending-summary-tile/spending-summary-tile';
import { SpendingService } from './spending-service';

@Component({
  selector: 'app-spending',
  imports: [
    PeriodSelector,
    SpendingSummaryTile,
    CategoryBreakdownList,
    SpendingSkeleton,
  ],
  templateUrl: './spending.html',
  styleUrl: './spending.css',
})
export class Spending implements OnInit {
  private readonly pageService = inject(PageService);
  private readonly spendingService = inject(SpendingService);

  readonly selectedPeriod = this.spendingService.selectedPeriod;
  readonly loading = this.spendingService.loading;
  readonly error = this.spendingService.error;
  readonly data = this.spendingService.data;

  readonly selectPeriod = (period: TimePeriod): void => this.spendingService.selectPeriod(period);

  readonly refetchAll = (): void => this.spendingService.refetchAll();

  ngOnInit(): void {
    this.pageService.setPageTitle('Spending');
    this.pageService.setPageSubtitle('View your spending summary and breakdown by category.');
  }
}
