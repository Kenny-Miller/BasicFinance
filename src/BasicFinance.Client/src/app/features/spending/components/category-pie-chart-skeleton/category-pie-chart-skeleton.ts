import { Component } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

@Component({
  selector: 'app-category-pie-chart-skeleton',
  imports: [HlmCardImports, HlmSkeletonImports],
  templateUrl: './category-pie-chart-skeleton.html',
  styleUrl: './category-pie-chart-skeleton.css',
})
export class CategoryPieChartSkeleton {}
