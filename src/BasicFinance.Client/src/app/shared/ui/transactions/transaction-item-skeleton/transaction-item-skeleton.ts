import { Component } from '@angular/core';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

@Component({
  selector: 'app-transaction-item-skeleton',
  imports: [HlmItemImports, HlmSkeletonImports],
  templateUrl: './transaction-item-skeleton.html',
  styleUrl: './transaction-item-skeleton.css',
})
export class TransactionItemSkeleton {}
