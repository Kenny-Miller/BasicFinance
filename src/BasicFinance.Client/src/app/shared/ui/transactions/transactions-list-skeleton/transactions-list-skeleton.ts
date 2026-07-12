import { Component, computed, input } from '@angular/core';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { TransactionItemSkeleton } from '../transaction-item-skeleton/transaction-item-skeleton';

@Component({
  selector: 'app-transactions-list-skeleton',
  imports: [HlmItemImports, HlmSeparatorImports, TransactionItemSkeleton],
  templateUrl: './transactions-list-skeleton.html',
  styleUrl: './transactions-list-skeleton.css',
})
export class TransactionsListSkeleton {
  readonly skeletonListLength = input<number>(10);
  readonly skeletonList = computed(() => Array.from({ length: this.skeletonListLength() }));
}
