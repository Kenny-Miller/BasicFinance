import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';

@Component({
  selector: 'app-spending-summary-tile',
  imports: [CommonModule, HlmCardImports],
  templateUrl: './spending-summary-tile.html',
  styleUrl: './spending-summary-tile.css',
})
export class SpendingSummaryTile {
  readonly totalIncome = input.required<number>();
  readonly totalSpend = input.required<number>();

  readonly netIncome = computed(() => this.totalIncome() - this.totalSpend());
}
