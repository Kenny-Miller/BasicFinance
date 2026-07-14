import { Component, inject, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmDatePickerImports } from '@spartan-ng/helm/date-picker';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import {
  TRANSACTION_CATEGORY_OPTIONS,
  TRANSACTION_TYPE_OPTIONS,
} from '../../data/transaction-options';
import { TransactionFilters } from '../../data/transactions-client';

@Component({
  selector: 'app-filter-bar',
  imports: [
    ReactiveFormsModule,
    HlmButtonImports,
    HlmInputImports,
    HlmDatePickerImports,
    HlmCardImports,
    HlmFieldImports,
    HlmSelectImports,
  ],
  templateUrl: './filter-bar.html',
  styleUrl: './filter-bar.css',
})
export class FilterBar {
  private readonly _formBuilder = inject(FormBuilder);

  public readonly filtersChange = output<TransactionFilters>();

  readonly transactionTypeOptions = TRANSACTION_TYPE_OPTIONS;
  readonly categoryOptions = TRANSACTION_CATEGORY_OPTIONS;

  public form = this._formBuilder.group({
    startDate: '',
    endDate: '',
    minAmount: '',
    maxAmount: '',
    transactionTypeId: [''],
    transactionCategoryId: [''],
    search: '',
  });

  onApply(): void {
    const v = this.form.value;
    const _toDate = (val: unknown): string =>
      val instanceof Date ? val.toISOString().split('T')[0] : (val as string) || '';
    this.filtersChange.emit({
      startDate: _toDate(v.startDate),
      endDate: _toDate(v.endDate),
      minAmount: v.minAmount || '',
      maxAmount: v.maxAmount || '',
      transactionTypeId: v.transactionTypeId || '',
      transactionCategoryId: v.transactionCategoryId || '',
      search: v.search || '',
    });
  }

  onReset(): void {
    this.form.reset({
      startDate: null,
      endDate: null,
      minAmount: '',
      maxAmount: '',
      transactionTypeId: '',
      transactionCategoryId: '',
      search: '',
    });

    this.filtersChange.emit(this.form
  }



  readonly transactionTypeToString = (value: string) =>
    this.transactionTypeOptions.find((d) => d.value === value)?.label ?? '';

  readonly categoryToString = (value: string) =>
    this.categoryOptions.find((d) => d.value === value)?.label ?? '';
}
