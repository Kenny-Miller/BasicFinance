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

  it('should prevent the native form submit and emit the current filters', () => {
    component.searchControl.setValue('coffee');
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    const event = new Event('submit', { bubbles: true, cancelable: true });

    form.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
    expect(emitted).toEqual([{ search: 'coffee' }]);
  });

  it('should keep the form in place when the Apply button is clicked', () => {
    component.searchControl.setValue('espresso');
    const button = fixture.nativeElement.querySelector(
      'button[type="submit"]',
    ) as HTMLButtonElement;

    button.click();

    expect(emitted).toEqual([{ search: 'espresso' }]);
  });

  it('should not emit and should mark controls as touched when the form is invalid', () => {
    component.minAmountControl.setValue(-5);
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;

    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

    expect(emitted).toEqual([]);
    expect(component.filterForm.controls['minAmount'].touched).toBe(true);
  });

  it('should clear the form and emit empty filters on reset', () => {
    component.searchControl.setValue('coffee');
    component.accountControl.setValue('account-1');

    component.resetFilters();

    expect(emitted).toEqual([{}]);
    expect(component.searchControl.value).toBe('');
    expect(component.accountControl.value).toBe('');
  });
});
