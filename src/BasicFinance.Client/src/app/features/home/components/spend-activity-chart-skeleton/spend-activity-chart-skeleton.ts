import { Component } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

@Component({
  selector: 'app-spend-activity-chart-skeleton',
  imports: [HlmCardImports, HlmSkeletonImports],
  templateUrl: './spend-activity-chart-skeleton.html',
  styleUrl: './spend-activity-chart-skeleton.css',
})
export class SpendActivityChartSkeleton {}
