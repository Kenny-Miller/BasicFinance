import { Component, inject, signal } from '@angular/core';
import { ThemeService } from '../../core/theme/theme.service';
import { CategoryBreakdownList } from './components/category-breakdown-list/category-breakdown-list';
import { CategoryBreakdownListSkeleton } from './components/category-breakdown-list-skeleton/category-breakdown-list-skeleton';
import { CategoryPieChart } from './components/category-pie-chart/category-pie-chart';
import { CategoryPieChartSkeleton } from './components/category-pie-chart-skeleton/category-pie-chart-skeleton';
import { PeriodSelector } from './components/period-selector/period-selector';
import { SpendingSummaryTile } from './components/spending-summary-tile/spending-summary-tile';
import { SpendingSummaryTileSkeleton } from './components/spending-summary-tile-skeleton/spending-summary-tile-skeleton';
import { SpendingClient, SpendingPeriod } from './data/spending-client';

@Component({
  selector: 'app-spending',
  imports: [
    PeriodSelector,
    SpendingSummaryTile,
    SpendingSummaryTileSkeleton,
    CategoryPieChart,
    CategoryPieChartSkeleton,
    CategoryBreakdownList,
    CategoryBreakdownListSkeleton,
  ],
  templateUrl: './spending.html',
  styleUrl: './spending.css',
})
export class Spending {
  readonly themeService = inject(ThemeService);
  private readonly spendingClient = inject(SpendingClient);

  readonly selectedPeriod = signal<SpendingPeriod>('Monthly');

  readonly startDate = signal(new Date().toISOString().split('T')[0]);

  readonly spendingResource = this.spendingClient.createResource(
    this.selectedPeriod,
    this.startDate,
  );

  selectPeriod(period: SpendingPeriod) {
    this.selectedPeriod.set(period);
  }

  retry() {
    this.startDate.update((v) => v);
  }
}
