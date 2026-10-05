import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CategoryBreakdownList } from './category-breakdown-list';
import { SpendingByPeriod } from '../../../../core/data-access/spending-client';

describe('CategoryBreakdownList', () => {
  let component: CategoryBreakdownList;
  let fixture: ComponentFixture<CategoryBreakdownList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CategoryBreakdownList],
    }).compileComponents();

    fixture = TestBed.createComponent(CategoryBreakdownList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render percentOfSpend as already a percentage value', () => {
    const data: SpendingByPeriod = {
      periodStartDate: '',
      periodEndDate: '',
      totalSpend: 100,
      totalIncome: 0,
      spendingActivityByCategory: {
        TAXES: { amount: 21.397, percentOfSpend: 21.397 },
      },
    };

    fixture.componentRef.setInput('categories', [{ id: 1, code: 'TAXES', name: 'Taxes' }]);
    fixture.componentRef.setInput('data', data);
    fixture.detectChanges();

    const content = fixture.nativeElement.innerHTML;
    expect(content).toContain('21.4%');
    expect(content).not.toContain('2,139.7%');
  });

  it('should use the category names from the categories input', () => {
    const data: SpendingByPeriod = {
      periodStartDate: '',
      periodEndDate: '',
      totalSpend: 100,
      totalIncome: 0,
      spendingActivityByCategory: {
        DINING: { amount: 40, percentOfSpend: 40 },
        UNC: { amount: 10, percentOfSpend: 10 },
      },
    };

    fixture.componentRef.setInput('categories', [
      { id: 1, code: 'DINING', name: 'Dining' },
      { id: 2, code: 'UNC', name: 'Uncategorized' },
    ]);
    fixture.componentRef.setInput('data', data);

    const rows = component.rows();

    expect(rows.map((row) => row.name)).toEqual(['Dining', 'Uncategorized']);
  });

  it('should fall back to the code when a category has no name', () => {
    const data: SpendingByPeriod = {
      periodStartDate: '',
      periodEndDate: '',
      totalSpend: 100,
      totalIncome: 0,
      spendingActivityByCategory: {
        TAXES: { amount: 5, percentOfSpend: 5 },
      },
    };

    fixture.componentRef.setInput('categories', []);
    fixture.componentRef.setInput('data', data);

    expect(component.rows()[0].name).toBe('TAXES');
  });

  it('should not include non-spending category codes in the rows', () => {
    const data: SpendingByPeriod = {
      periodStartDate: '',
      periodEndDate: '',
      totalSpend: 100,
      totalIncome: 0,
      spendingActivityByCategory: {
        AUTO: { amount: 25, percentOfSpend: 25 },
        INCOME: { amount: 15, percentOfSpend: 15 },
      },
    };

    fixture.componentRef.setInput('categories', [{ id: 1, code: 'AUTO', name: 'Auto and Transport' }]);
    fixture.componentRef.setInput('data', data);

    expect(component.rows().map((row) => row.code)).toEqual(['AUTO']);
  });
});
