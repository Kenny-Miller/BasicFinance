import { Component } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

@Component({
  selector: 'app-summary-card-skeleton',
  imports: [HlmCardImports, HlmSkeletonImports],
  templateUrl: './summary-card-skeleton.html',
  styleUrl: './summary-card-skeleton.css',
})
export class SummaryCardSkeleton {}
