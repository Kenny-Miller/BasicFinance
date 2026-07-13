import { Component } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

@Component({
  selector: 'app-spending-summary-tile-skeleton',
  imports: [HlmCardImports, HlmSkeletonImports],
  templateUrl: './spending-summary-tile-skeleton.html',
  styleUrl: './spending-summary-tile-skeleton.css',
})
export class SpendingSummaryTileSkeleton {}
