import { CommonModule } from '@angular/common';
import { Component, input } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';

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
  readonly variant = input<Variant>('category');

  get percentChange(): number {
    const last = this.lastMonthValue();
    if (last === 0) {
      return this.currentValue() > 0 ? 100 : 0;
    }
    return ((this.currentValue() - last) / last) * 100;
  }

  get changeClass(): string {
    return this.currentValue() >= this.lastMonthValue() ? 'text-emerald-500' : 'text-red-500';
  }

  get changeSign(): string {
    return this.currentValue() >= this.lastMonthValue() ? '+' : '';
  }

  get dollarDifference(): number {
    return Math.abs(this.currentValue() - this.lastMonthValue());
  }

  get isIncrease(): boolean {
    return this.currentValue() >= this.lastMonthValue();
  }
}
