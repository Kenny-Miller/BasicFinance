import { Component, inject } from '@angular/core';

import { PageService } from '../../core/page/page.service';
import { AppPageHeader } from '../../shared/ui/app-page-header/app-page-header';
import { PeriodStepper } from '../../shared/ui/period-stepper/period-stepper';
import { CategoryBreakdownList } from './components/category-breakdown-list/category-breakdown-list';
import { SpendingSkeleton } from './components/spending-skeleton/spending-skeleton';
import { SpendingSummaryTile } from './components/spending-summary-tile/spending-summary-tile';
import { SpendingService } from './spending-service';

@Component({
  selector: 'app-spending',
  imports: [
    PeriodStepper,
    SpendingSummaryTile,
    CategoryBreakdownList,
    SpendingSkeleton,
    AppPageHeader,
  ],
  templateUrl: './spending.html',
  styleUrl: './spending.css',
})
export class Spending {
  private readonly pageService = inject(PageService);
  private readonly spendingService = inject(SpendingService);

  readonly selectedTimePeriod = this.spendingService.selectedTimePeriod;
  readonly selectedDate = this.spendingService.selectedDate;
  readonly loading = this.spendingService.loading;
  readonly error = this.spendingService.error;
  readonly data = this.spendingService.data;
  readonly categories = this.pageService.transactionCategories;

  readonly refetchAll = (): void => this.spendingService.refetchAll();
}
