import { Component } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSkeletonImports } from '@spartan-ng/helm/skeleton';

@Component({
  selector: 'app-account-net-worth-breakdown-skeleton',
  imports: [HlmCardImports, HlmSkeletonImports],
  templateUrl: './account-net-worth-breakdown-skeleton.html',
  styleUrl: './account-net-worth-breakdown-skeleton.css',
})
export class AccountNetWorthBreakdownSkeleton {}
