import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AccountTypeClient } from '../../core/data-access/account-type-client';
import { InstitutionClient } from '../../core/data-access/institution-client';
import { TransactionCategoryClient } from '../../core/data-access/transaction-category-client';
import { TransactionTypeClient } from '../../core/data-access/transaction-type-client';
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
        {
          provide: InstitutionClient,
          useValue: {
            myInstitutions: () => ({
              hasValue: () => true,
              value: () => [],
              error: () => null,
              reload: () => true,
            }),
          },
        },
        {
          provide: AccountTypeClient,
          useValue: {
            accountTypes: () => ({
              hasValue: () => true,
              value: () => [],
              error: () => null,
              reload: () => true,
            }),
          },
        },
        {
          provide: TransactionTypeClient,
          useValue: {
            transactionTypes: () => ({
              hasValue: () => true,
              value: () => [],
              error: () => null,
              reload: () => true,
            }),
          },
        },
        {
          provide: TransactionCategoryClient,
          useValue: {
            transactionCategories: () => ({
              hasValue: () => true,
              value: () => [],
              error: () => null,
              reload: () => true,
            }),
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
