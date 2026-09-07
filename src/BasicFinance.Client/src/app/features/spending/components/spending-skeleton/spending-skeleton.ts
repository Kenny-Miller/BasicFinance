import { Component } from '@angular/core';
import { CategoryBreakdownListSkeleton } from '../category-breakdown-list-skeleton/category-breakdown-list-skeleton';
import { SpendingSummaryTileSkeleton } from '../spending-summary-tile-skeleton/spending-summary-tile-skeleton';

@Component({
  selector: 'app-spending-skeleton',
  imports: [SpendingSummaryTileSkeleton, CategoryBreakdownListSkeleton],
  templateUrl: './spending-skeleton.html',
  styleUrl: './spending-skeleton.css',
})
export class SpendingSkeleton {}
