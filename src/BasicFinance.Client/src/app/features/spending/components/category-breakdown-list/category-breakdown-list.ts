import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { TransactionCategory } from '../../../../core/data-access/transaction-category-client';
import { SpendingByPeriod } from '../../../../core/data-access/spending-client';
import { SPENDING_CATEGORY_CODES } from '../../data/spending-categories';

export interface CategoryRow {
  code: string;
  name: string;
  amount: number;
  percentOfSpend: number;
}

@Component({
  selector: 'app-category-breakdown-list',
  imports: [CommonModule, HlmCardImports],
  templateUrl: './category-breakdown-list.html',
  styleUrl: './category-breakdown-list.css',
})
export class CategoryBreakdownList {
  readonly data = input<SpendingByPeriod | undefined>();
  readonly categories = input<TransactionCategory[]>([]);

  readonly categoryNames = computed<Record<string, string>>(() =>
    Object.fromEntries(this.categories().map((category) => [category.code, category.name] as [string, string])),
  );

  readonly rows = computed<CategoryRow[]>(() => {
    const spending = this.data();
    if (!spending || !spending.spendingActivityByCategory) {
      return [];
    }

    return Object.entries(spending.spendingActivityByCategory)
      .filter(([code]) => SPENDING_CATEGORY_CODES.has(code))
      .map(([code, activity]) => ({
        code,
        name: this.categoryNames()[code] ?? code,
        amount: activity.amount,
        percentOfSpend: activity.percentOfSpend,
      }))
      .filter((row) => row.amount > 0)
      .sort((a, b) => b.amount - a.amount);
  });

  readonly totalSpend = computed(() => {
    return this.rows().reduce((sum, row) => sum + row.amount, 0);
  });
}
