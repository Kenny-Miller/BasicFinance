import { Component } from '@angular/core';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

@Component({
  selector: 'app-recent-transactions-skeleton',
  imports: [HlmCardImports, HlmSkeletonImports, HlmButtonImports],
  templateUrl: './recent-transactions-skeleton.html',
  styleUrl: './recent-transactions-skeleton.css',
})
export class RecentTransactionsSkeleton {}
