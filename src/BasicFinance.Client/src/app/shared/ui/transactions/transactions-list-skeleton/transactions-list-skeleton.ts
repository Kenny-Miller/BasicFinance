import { Component, computed, input } from '@angular/core';
import { TransactionItemSkeleton } from '../transaction-item-skeleton/transaction-item-skeleton';

@Component({
  selector: 'app-transactions-list-skeleton',
  imports: [TransactionItemSkeleton],
  templateUrl: './transactions-list-skeleton.html',
  styleUrl: './transactions-list-skeleton.css',
})
export class TransactionsListSkeleton {
  numberOfSkeletons = input<number>(5);
  skeletons = computed(() => Array.from({ length: this.numberOfSkeletons() }, (_, i) => i));
}
