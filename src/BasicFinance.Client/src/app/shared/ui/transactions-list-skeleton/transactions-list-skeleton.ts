import { Component, computed, input } from '@angular/core';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';

@Component({
  selector: 'app-transactions-list-skeleton',
  imports: [HlmSkeletonImports, HlmItemImports, HlmSeparatorImports],
  templateUrl: './transactions-list-skeleton.html',
  styleUrl: './transactions-list-skeleton.css',
})
export class TransactionsListSkeleton {
  readonly skeletonListLength = input<number>(10);
  readonly skeletonList = computed(() => Array.from({ length: this.skeletonListLength() }));
}
