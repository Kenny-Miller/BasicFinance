import { Component } from '@angular/core';
import { SummaryCardSkeleton } from '../../../../shared/ui/cards/summary-card-skeleton/summary-card-skeleton';
import { TransactionItemSkeleton } from '../../../../shared/ui/transactions/transaction-item-skeleton/transaction-item-skeleton';
import { DailySpendChartSkeleton } from '../daily-spend-chart-skeleton/daily-spend-chart-skeleton';

@Component({
  selector: 'app-transactions-skeleton',
  imports: [SummaryCardSkeleton, DailySpendChartSkeleton, TransactionItemSkeleton],
  templateUrl: './transactions-skeleton.html',
  styleUrl: './transactions-skeleton.css',
})
export class TransactionsSkeleton {}
