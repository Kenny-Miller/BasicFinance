import { Component } from '@angular/core';
import { SummaryCardSkeleton } from '../../../../shared/ui/cards/summary-card-skeleton/summary-card-skeleton';
import { TransactionsListSkeleton } from '../../../../shared/ui/transactions/transactions-list-skeleton/transactions-list-skeleton';
import { DailySpendChartSkeleton } from '../daily-spend-chart-skeleton/daily-spend-chart-skeleton';

@Component({
  selector: 'app-transactions-skeleton',
  imports: [SummaryCardSkeleton, DailySpendChartSkeleton, TransactionsListSkeleton],
  templateUrl: './transactions-skeleton.html',
  styleUrl: './transactions-skeleton.css',
})
export class TransactionsSkeleton {}
