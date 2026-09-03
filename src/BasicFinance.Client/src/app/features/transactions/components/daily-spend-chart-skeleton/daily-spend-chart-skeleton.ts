import { Component } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

@Component({
  selector: 'app-daily-spend-chart-skeleton',
  imports: [HlmCardImports, HlmSkeletonImports],
  templateUrl: './daily-spend-chart-skeleton.html',
  styleUrl: './daily-spend-chart-skeleton.css',
})
export class DailySpendChartSkeleton {}
