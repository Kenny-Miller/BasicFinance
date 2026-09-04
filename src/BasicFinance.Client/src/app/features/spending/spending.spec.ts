import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TimePeriod } from '../../shared/data/time-period';
import { Spending } from './spending';
import { SpendingService } from './spending-service';

describe('Spending', () => {
  let component: Spending;
  let fixture: ComponentFixture<Spending>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Spending],
      providers: [
        {
          provide: SpendingService,
          useValue: {
            selectedPeriod: signal<TimePeriod>('Monthly'),
            loading: () => false,
            error: () => undefined,
            data: () => ({
              periodStartDate: '',
              periodEndDate: '',
              totalSpend: 0,
              totalIncome: 0,
              spendingActivityByCategory: {},
            }),
            selectPeriod: () => undefined,
            refetchAll: () => undefined,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Spending);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
