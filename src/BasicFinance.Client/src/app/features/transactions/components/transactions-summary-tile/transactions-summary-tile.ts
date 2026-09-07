import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';

@Component({
  selector: 'app-transactions-summary-tile',
  imports: [CommonModule, HlmCardImports],
  templateUrl: './transactions-summary-tile.html',
  styleUrl: './transactions-summary-tile.css',
})
export class TransactionsSummaryTile {
  readonly title = input.required<string>();
  readonly currentValue = input.required<number>();
  readonly lastPeriodValue = input.required<number>();
  readonly deltaLabel = input<string>('last month');
  readonly positiveIsGood = input<boolean>(true);
  readonly isCurrency = input<boolean>(true);

  readonly valueDifference = computed(() => this.currentValue() - this.lastPeriodValue());

  private readonly percentFormatter = new Intl.NumberFormat('en-US', {
    style: 'percent',
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  });
  private readonly currencyFormatter = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  });
  private readonly numberFormatter = new Intl.NumberFormat('en-US');

  readonly deltaText = computed(() => {
    const difference = this.valueDifference();
    const lastPeriodValue = this.lastPeriodValue();

    if (difference === 0 && lastPeriodValue <= 0) {
      return 'No change';
    }

    const sign = difference >= 0 ? '+' : '-';
    const magnitude = Math.abs(difference);
    const formatted = this.isCurrency()
      ? this.currencyFormatter.format(magnitude)
      : this.numberFormatter.format(magnitude);

    if (lastPeriodValue <= 0) {
      return `${sign}${formatted}`;
    }

    const percent = this.percentFormatter.format(magnitude / lastPeriodValue);
    return `${sign}${formatted} (${sign}${percent})`;
  });

  readonly isFavorable = computed(() =>
    this.positiveIsGood() ? this.valueDifference() >= 0 : this.valueDifference() <= 0,
  );

  readonly topBarClass = computed(() => (this.isFavorable() ? 'bg-emerald-500' : 'bg-red-500'));
  readonly footerClass = computed(() => (this.isFavorable() ? 'text-emerald-500' : 'text-red-500'));
}
