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

    fixture.componentRef.setInput('data', data);
    fixture.detectChanges();

    const content = fixture.nativeElement.innerHTML;
    expect(content).toContain('21.4%');
    expect(content).not.toContain('2,139.7%');
  });
});
