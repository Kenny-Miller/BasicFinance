import { Component } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';
import { SummaryCardSkeleton } from '../../../../shared/ui/cards/summary-card-skeleton/summary-card-skeleton';

@Component({
  selector: 'app-account-skeleton',
  imports: [SummaryCardSkeleton, HlmCardImports, HlmSkeletonImports],
  templateUrl: './account-skeleton.html',
  styleUrl: './account-skeleton.css',
})
export class AccountSkeleton {}
