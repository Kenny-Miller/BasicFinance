import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Transactions } from './transactions';
import { TransactionsClient } from './data/transactions-client';

describe('Transactions', () => {
  let component: Transactions;
  let fixture: ComponentFixture<Transactions>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Transactions],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        {
          provide: TransactionsClient,
          useValue: {
            createResource: () => ({
              value: () => ({ items: [], totalCount: 0, page: 1 }),
              hasValue: () => false,
              error: () => null,
              isLoading: () => false,
            }),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Transactions);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
