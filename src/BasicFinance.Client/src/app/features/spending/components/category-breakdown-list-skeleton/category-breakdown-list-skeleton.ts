import { Component } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

@Component({
  selector: 'app-category-breakdown-list-skeleton',
  imports: [HlmCardImports, HlmSkeletonImports],
  templateUrl: './category-breakdown-list-skeleton.html',
  styleUrl: './category-breakdown-list-skeleton.css',
})
export class CategoryBreakdownListSkeleton {}
