import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';

@Component({
  selector: 'app-summary-card',
  imports: [CommonModule, HlmCardImports],
  templateUrl: './summary-card.html',
  styleUrl: './summary-card.css',
})
export class SummaryCard {
  readonly title = input.required<string>();
  readonly currentPeriodValue = input.required<number>();
  readonly previousPeriodValue = input.required<number>();

  readonly valueDifference = computed(() => this.currentPeriodValue() - this.previousPeriodValue());

  private readonly percentFormatter = new Intl.NumberFormat('en-US', {
    style: 'percent',
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  });
  private readonly currencyFormatter = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  });

  readonly deltaText = computed(() => {
    const difference = this.valueDifference();
    const lastPeriodValue = this.previousPeriodValue();

    if (difference === 0 && lastPeriodValue <= 0) {
      return 'No change';
    }

    const sign = difference >= 0 ? '+' : '-';
    const magnitude = Math.abs(difference);
    const percent = this.percentFormatter.format(magnitude / lastPeriodValue);

    if (lastPeriodValue <= 0) {
      return `${sign}${this.currencyFormatter.format(magnitude)}`;
    }

    return `${sign}${percent}`;
  });

  readonly isFavorable = computed(() => this.valueDifference() >= 0);

  readonly topBarClass = computed(() => (this.isFavorable() ? 'bg-emerald-500' : 'bg-red-500'));
  readonly footerClass = computed(() => (this.isFavorable() ? 'text-emerald-500' : 'text-red-500'));
}
