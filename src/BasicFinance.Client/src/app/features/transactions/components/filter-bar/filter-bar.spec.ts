import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TransactionFilters } from '../../../../core/data-access/transaction-client';
import { FilterBar } from './filter-bar';

describe('FilterBar', () => {
  let fixture: ComponentFixture<FilterBar>;
  let component: FilterBar;
  const emitted: TransactionFilters[] = [];

  beforeEach(async () => {
    emitted.length = 0;

    await TestBed.configureTestingModule({
      imports: [FilterBar],
    }).compileComponents();

    fixture = TestBed.createComponent(FilterBar);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('filters', {});
    component.filterChange.subscribe((filters) => emitted.push(filters));
    fixture.detectChanges();
  });

  const submitForm = () => {
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    const event = new Event('submit', { bubbles: true, cancelable: true });
    form.dispatchEvent(event);
    return event;
  };

  it('should prevent the native form submit and emit the current filters', () => {
    component.filterModel.update((draft) => ({ ...draft, search: 'coffee' }));
    const event = submitForm();

    expect(event.defaultPrevented).toBe(true);
    expect(emitted).toEqual([{ search: 'coffee' }]);
  });

  it('should keep the form in place when the Apply button is clicked', () => {
    component.filterModel.update((draft) => ({ ...draft, search: 'espresso' }));
    const button = fixture.nativeElement.querySelector(
      'button[type="submit"]',
    ) as HTMLButtonElement;

    button.click();

    expect(emitted).toEqual([{ search: 'espresso' }]);
  });

  it('should not emit and should mark fields as touched when the form is invalid', () => {
    component.filterModel.update((draft) => ({ ...draft, minAmount: -5 }));

    submitForm();

    expect(emitted).toEqual([]);
    expect(component.filterForm().invalid()).toBe(true);
    expect(component.filterForm.minAmount().touched()).toBe(true);
  });

  it('should not emit when only the draft changes', () => {
    component.filterModel.update((draft) => ({ ...draft, search: 'latte' }));

    expect(emitted).toEqual([]);
  });

  it('should sync the draft when the applied filters change', () => {
    fixture.componentRef.setInput('filters', { search: 'coffee', minAmount: 10 });
    fixture.detectChanges();

    expect(component.filterModel().search).toBe('coffee');
    expect(component.filterModel().minAmount).toBe(10);
  });

  it('should emit numeric amounts from the draft', () => {
    component.filterModel.update((draft) => ({ ...draft, minAmount: 10, maxAmount: 99.99 }));

    submitForm();

    expect(emitted).toEqual([{ minAmount: 10, maxAmount: 99.99 }]);
  });

  it('should omit amount filters when the draft amounts are empty', () => {
    component.filterModel.update((draft) => ({ ...draft, minAmount: Number.NaN, maxAmount: Number.NaN }));

    submitForm();

    expect(emitted).toEqual([{}]);
  });

  it('should read typed amounts as numbers from the native number input', () => {
    const input = fixture.nativeElement.querySelector('#minimum-amount') as HTMLInputElement;

    input.value = '42';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();

    expect(component.filterModel().minAmount).toBe(42);

    input.value = '';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();

    expect(Number.isNaN(component.filterModel().minAmount)).toBe(true);
  });

  it('should show only the placeholder options until the reference lists are provided', () => {
    expect(component.typeOptions()).toEqual([{ value: '', label: 'All Types' }]);
    expect(component.categoryOptions()).toEqual([{ value: '', label: 'All Categories' }]);
  });

  it('should build the type and category options from the reference inputs, placeholder first', () => {
    fixture.componentRef.setInput('transactionTypes', [
      { id: 1, code: 'CR', name: 'Credit' },
      { id: 2, code: 'DR', name: 'Debit' },
    ]);
    fixture.componentRef.setInput('transactionCategories', [
      { id: 1, code: 'UNC', name: 'Uncategorized' },
      { id: 2, code: 'DINING', name: 'Dining' },
    ]);

    expect(component.typeOptions()).toEqual([
      { value: '', label: 'All Types' },
      { value: 'CR', label: 'Credit' },
      { value: 'DR', label: 'Debit' },
    ]);
    expect(component.categoryOptions()).toEqual([
      { value: '', label: 'All Categories' },
      { value: 'UNC', label: 'Uncategorized' },
      { value: 'DINING', label: 'Dining' },
    ]);
  });

  it('should resolve type and category codes to labels, falling back to the code', () => {
    fixture.componentRef.setInput('transactionTypes', [{ id: 1, code: 'CR', name: 'Credit' }]);
    fixture.componentRef.setInput('transactionCategories', [{ id: 1, code: 'TAXES', name: 'Taxes' }]);

    expect(component.typeItemToString('CR')).toBe('Credit');
    expect(component.typeItemToString('')).toBe('All Types');
    expect(component.typeItemToString('UNKNOWN')).toBe('UNKNOWN');
    expect(component.categoryItemToString('TAXES')).toBe('Taxes');
    expect(component.categoryItemToString('')).toBe('All Categories');
    expect(component.categoryItemToString('UNKNOWN')).toBe('UNKNOWN');
  });

  it('should clear the form and emit empty filters on reset', () => {
    component.filterModel.update((draft) => ({
      ...draft,
      search: 'coffee',
      accountId: 'account-1',
    }));

    component.resetFilters();

    expect(emitted).toEqual([{}]);
    expect(component.filterModel().search).toBe('');
    expect(component.filterModel().accountId).toBe('');
    expect(Number.isNaN(component.filterModel().minAmount)).toBe(true);
    expect(component.filterForm().dirty()).toBe(false);
  });
});
