import { Component, inject, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { BrnSelectImports } from '@spartan-ng/brain/select';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputImports } from '@spartan-ng/helm/input';
import { HlmItemImports } from '@spartan-ng/helm/item';
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
    BrnSelectImports,
    HlmCardImports,
    HlmFieldImports,
    HlmSelectImports,
    HlmItemImports,
  ],
  templateUrl: './filter-bar.html',
  styleUrl: './filter-bar.css',
})
export class FilterBar {
  private readonly _formBuilder = inject(FormBuilder);

  public readonly filtersChange = output<TransactionFilters>();

  readonly typeOptions = TRANSACTION_TYPE_OPTIONS;
  readonly categoryOptions = TRANSACTION_CATEGORY_OPTIONS;

  public form = this._formBuilder.group({
    startDate: [''],
    endDate: [''],
    minAmount: [''],
    maxAmount: [''],
    transactionTypeId: [''],
    transactionCategoryId: [''],
    search: [''],
  });

  onApply(): void {
    this.filtersChange.emit(this.form.value as TransactionFilters);
  }

  onReset(): void {
    this.form.reset({
      startDate: '',
      endDate: '',
      minAmount: '',
      maxAmount: '',
      transactionTypeId: '',
      transactionCategoryId: '',
      search: '',
    });
  }
}
