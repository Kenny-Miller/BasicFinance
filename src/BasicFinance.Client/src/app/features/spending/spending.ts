import { Component, inject, OnInit, signal } from '@angular/core';
import { PageService } from '../../core/page/page.service';
import { ThemeService } from '../../core/theme/theme.service';
import { CategoryBreakdownListSkeleton } from './components/category-breakdown-list-skeleton/category-breakdown-list-skeleton';
import { CategoryBreakdownList } from './components/category-breakdown-list/category-breakdown-list';
import { CategoryPieChartSkeleton } from './components/category-pie-chart-skeleton/category-pie-chart-skeleton';
import { CategoryPieChart } from './components/category-pie-chart/category-pie-chart';
import { PeriodSelector } from './components/period-selector/period-selector';
import { SpendingSummaryTileSkeleton } from './components/spending-summary-tile-skeleton/spending-summary-tile-skeleton';
import { SpendingSummaryTile } from './components/spending-summary-tile/spending-summary-tile';
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
export class Spending implements OnInit {
  private readonly pageService = inject(PageService);
  private readonly spendingClient = inject(SpendingClient);
  private readonly themeService = inject(ThemeService);

  readonly appTheme = this.themeService.appTheme;
  readonly selectedPeriod = signal<SpendingPeriod>('Monthly');
  readonly startDate = signal(new Date().toISOString().split('T')[0]);
  readonly spendingResource = this.spendingClient.createResource(
    this.selectedPeriod,
    this.startDate,
  );

  ngOnInit(): void {
    this.pageService.setPageTitle('Spending');
    this.pageService.setPageSubtitle('View your spending summary and breakdown by category.');
  }

  selectPeriod(period: SpendingPeriod) {
    this.selectedPeriod.set(period);
  }

  retry() {
    this.startDate.update((v) => v);
  }
}
