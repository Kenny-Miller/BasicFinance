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
    component.filterModel.update((draft) => ({ ...draft, minAmount: '-5' }));

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
    expect(component.filterModel().minAmount).toBe('10');
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
    expect(component.filterForm().dirty()).toBe(false);
  });
});
