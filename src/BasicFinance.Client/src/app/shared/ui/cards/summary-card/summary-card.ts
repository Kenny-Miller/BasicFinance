import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSeparator } from '@spartan-ng/helm/separator';

type Variant = 'networth' | 'category';

@Component({
  selector: 'app-summary-card',
  imports: [CommonModule, HlmCardImports],
  templateUrl: './summary-card.html',
  styleUrl: './summary-card.css',
})
export class SummaryCard {
  readonly title = input.required<string>();
  readonly currentValue = input.required<number>();
  readonly lastMonthValue = input.required<number>();
  readonly showTopBar = input<boolean>(true);

  readonly valueDifference = computed(() => this.currentValue() - this.lastMonthValue());
  readonly valueSign = computed(() => (this.valueDifference() >= 0 ? '+' : '-'));
  readonly valuePercentChange = computed(() => {
    const lastMonthValue = this.lastMonthValue();
    return lastMonthValue > 0
      ? (this.valueDifference() / lastMonthValue) * 100
      : this.valueDifference();
  });

  readonly highlightColor = computed(() =>
    this.valueDifference() >= 0 ? 'emerald-500' : 'red-500',
  );
}
