import { Component } from '@angular/core';
import { SummaryCardSkeleton } from '../../../../shared/ui/cards/summary-card-skeleton/summary-card-skeleton';
import { AccountNetWorthBreakdownSkeleton } from '../account-net-worth-breakdown-skeleton/account-net-worth-breakdown-skeleton';
import { RecentTransactionsSkeleton } from '../recent-transactions-skeleton/recent-transactions-skeleton';
import { SpendActivityChartSkeleton } from '../spend-activity-chart-skeleton/spend-activity-chart-skeleton';

@Component({
  selector: 'app-home-skeleton',
  imports: [
    SummaryCardSkeleton,
    SpendActivityChartSkeleton,
    AccountNetWorthBreakdownSkeleton,
    RecentTransactionsSkeleton,
  ],
  templateUrl: './home-skeleton.html',
  styleUrl: './home-skeleton.css',
})
export class HomeSkeleton {}
