import { Component, computed, effect, input, output, signal } from '@angular/core';
import { Field, form, min, submit } from '@angular/forms/signals';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmDatePickerImports } from '@spartan-ng/helm/date-picker';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import { Account } from '../../../../core/data-access/account-client';
import {
  TransactionFilters,
  TransactionTypeCode,
} from '../../../../core/data-access/transaction-client';
import {
  SelectOption,
  TRANSACTION_TYPE_OPTIONS,
} from '../../../../shared/data/transaction-type-map';
import { TRANSACTION_CATEGORY_OPTIONS } from '../../data/transaction-options';

interface FilterDraft {
  startDate: Date | null;
  endDate: Date | null;
  minAmount: string;
  maxAmount: string;
  transactionTypeCode: string;
  transactionCategoryCode: string;
  accountId: string;
  search: string;
}

@Component({
  selector: 'app-filter-bar',
  imports: [
    Field,
    HlmDatePickerImports,
    HlmButtonImports,
    HlmInputImports,
    HlmSelectImports,
    HlmFieldImports,
  ],
  templateUrl: './filter-bar.html',
  styleUrl: './filter-bar.css',
})
export class FilterBar {
  readonly filters = input.required<TransactionFilters>();
  readonly accounts = input<Account[]>([]);

  readonly filterChange = output<TransactionFilters>();

  readonly filterModel = signal<FilterDraft>(this._draftFrom({}));
  readonly filterForm = form(this.filterModel, (draft) => {
    min(draft.minAmount, 0);
    min(draft.maxAmount, 0);
  });

  readonly typeOptions: SelectOption[] = TRANSACTION_TYPE_OPTIONS;
  readonly categoryOptions: SelectOption[] = TRANSACTION_CATEGORY_OPTIONS;
  readonly accountOptions = computed<SelectOption[]>(() => [
    { value: '', label: 'All Accounts' },
    ...this.accounts().map((account) => ({
      value: account.id,
      label: account.name,
    })),
  ]);

  readonly typeItemToString = (code: string) => this._labelFor(this.typeOptions, code);
  readonly categoryItemToString = (code: string) => this._labelFor(this.categoryOptions, code);
  readonly accountItemToString = (code: string) => this._labelFor(this.accountOptions(), code);

  minDate = new Date(2025, 0, 1);

  private readonly lastSynced = signal<TransactionFilters>({});

  constructor() {
    effect(() => {
      const filters = this.filters();
      if (JSON.stringify(filters) === JSON.stringify(this.lastSynced())) {
        return;
      }

      this.lastSynced.set(filters);
      this.filterModel.set(this._draftFrom(filters));
    });
  }

  applyFilters(event: Event): void {
    event.preventDefault();

    void submit(this.filterForm, async () => {
      this.filterChange.emit(this._toFilters(this.filterModel()));
      return undefined;
    });
  }

  resetFilters(): void {
    this.filterModel.set(this._draftFrom({}));
    this.filterForm().reset();
    this.filterChange.emit({});
  }

  private _draftFrom(filters: TransactionFilters): FilterDraft {
    return {
      startDate: this._parseDate(filters.startDate),
      endDate: this._parseDate(filters.endDate),
      minAmount: this._toDraftAmount(filters.minAmount),
      maxAmount: this._toDraftAmount(filters.maxAmount),
      transactionTypeCode: filters.transactionTypeCode ?? '',
      transactionCategoryCode: filters.transactionCategoryCode ?? '',
      accountId: filters.accountId ?? '',
      search: filters.search ?? '',
    };
  }

  private _toFilters(draft: FilterDraft): TransactionFilters {
    return {
      startDate: this._toDate(draft.startDate),
      endDate: this._toDate(draft.endDate),
      minAmount: this._toNumber(draft.minAmount),
      maxAmount: this._toNumber(draft.maxAmount),
      transactionTypeCode: this._toTypeCode(draft.transactionTypeCode),
      transactionCategoryCode: draft.transactionCategoryCode || undefined,
      accountId: draft.accountId || undefined,
      search: draft.search.trim() || undefined,
    };
  }

  private _toDate(value: unknown): string | undefined {
    if (value instanceof Date) {
      return value.toISOString().split('T')[0];
    }

    if (typeof value === 'string' && value.length > 0) {
      return value;
    }

    return undefined;
  }

  private _toDraftAmount(value: number | undefined): string {
    return value != null ? String(value) : '';
  }

  private _toNumber(value: string): number | undefined {
    if (value.trim() === '') {
      return undefined;
    }

    const parsed = Number(value);
    return Number.isNaN(parsed) ? undefined : parsed;
  }

  private _toTypeCode(value: string): TransactionTypeCode | undefined {
    return value === 'CR' || value === 'DR' ? value : undefined;
  }

  private _labelFor(options: SelectOption[], code: string): string {
    return options.find((option) => option.value === code)?.label ?? code;
  }

  private _parseDate(value: string | undefined): Date | null {
    if (!value) {
      return null;
    }

    const [year, month, day] = value.split('-').map(Number);
    return new Date(year, month - 1, day);
  }
}
