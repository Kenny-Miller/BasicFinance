import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, OnInit, Output, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
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

@Component({
  selector: 'app-filter-bar',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    HlmDatePickerImports,
    HlmButtonImports,
    HlmInputImports,
    HlmSelectImports,
    HlmFieldImports,
  ],
  templateUrl: './filter-bar.html',
  styleUrl: './filter-bar.css',
})
export class FilterBar implements OnInit, OnChanges {
  @Input({ required: true }) filters!: TransactionFilters;
  @Input() accounts: Account[] = [];
  @Output() filterChange = new EventEmitter<TransactionFilters>();

  private readonly formBuilder = inject(NonNullableFormBuilder);
  readonly filterForm = this.formBuilder.group({
    startDate: [null as Date | null],
    endDate: [null as Date | null],
    minAmount: [null as number | null, [Validators.min(0)]],
    maxAmount: [null as number | null, [Validators.min(0)]],
    transactionTypeCode: [''],
    transactionCategoryCode: [''],
    accountId: [''],
    search: [''],
  });
  readonly typeOptions: SelectOption[] = TRANSACTION_TYPE_OPTIONS;
  readonly categoryOptions: SelectOption[] = TRANSACTION_CATEGORY_OPTIONS;
  accountOptions: SelectOption[] = [{ value: '', label: 'All Accounts' }];

  readonly typeItemToString = (code: string) => this._labelFor(this.typeOptions, code);
  readonly categoryItemToString = (code: string) => this._labelFor(this.categoryOptions, code);
  readonly accountItemToString = (code: string) => this._labelFor(this.accountOptions, code);

  minDate = new Date(2025, 0, 1);

  ngOnInit(): void {
    this.syncFromFilters(this.filters);
  }

  ngOnChanges(): void {
    this.accountOptions = [
      { value: '', label: 'All Accounts' },
      ...this.accounts.map((account) => ({
        value: account.id,
        label: account.name,
      })),
    ];
    this.syncFromFilters(this.filters);
  }

  private syncFromFilters(filters: TransactionFilters): void {
    this.startDateControl.setValue(this._parseDate(filters.startDate));
    this.endDateControl.setValue(this._parseDate(filters.endDate));
    this.minAmountControl.setValue(filters.minAmount ?? null);
    this.maxAmountControl.setValue(filters.maxAmount ?? null);
    this.typeControl.setValue(filters.transactionTypeCode ?? '');
    this.categoryControl.setValue(filters.transactionCategoryCode ?? '');
    this.accountControl.setValue(filters.accountId ?? '');
    this.searchControl.setValue(filters.search ?? '');
  }

  applyFilters(event: Event): void {
    event.preventDefault();

    if (!this.filterForm.valid) {
      this.filterForm.markAllAsTouched();
      return;
    }

    const value = this.filterForm.getRawValue();

    const filters: TransactionFilters = {
      startDate: this._toDate(value.startDate),
      endDate: this._toDate(value.endDate),
      minAmount: value.minAmount ?? undefined,
      maxAmount: value.maxAmount ?? undefined,
      transactionTypeCode: this._toTypeCode(value.transactionTypeCode),
      transactionCategoryCode: value.transactionCategoryCode || undefined,
      accountId: value.accountId || undefined,
      search: value.search.trim() || undefined,
    };

    this.filterChange.emit(filters);
  }

  resetFilters(): void {
    this.syncFromFilters({});
    this.filterChange.emit({});
  }

  get startDateControl() {
    return this.filterForm.controls['startDate'];
  }

  get endDateControl() {
    return this.filterForm.controls['endDate'];
  }

  get minAmountControl() {
    return this.filterForm.controls['minAmount'];
  }

  get maxAmountControl() {
    return this.filterForm.controls['maxAmount'];
  }

  get typeControl() {
    return this.filterForm.controls['transactionTypeCode'];
  }

  get categoryControl() {
    return this.filterForm.controls['transactionCategoryCode'];
  }

  get accountControl() {
    return this.filterForm.controls['accountId'];
  }

  get searchControl() {
    return this.filterForm.controls['search'];
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
