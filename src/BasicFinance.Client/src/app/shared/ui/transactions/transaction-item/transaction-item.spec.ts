import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TransactionItem } from './transaction-item';

describe('TransactionItem', () => {
  let component: TransactionItem;
  let fixture: ComponentFixture<TransactionItem>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TransactionItem],
    }).compileComponents();

    fixture = TestBed.createComponent(TransactionItem);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('transaction', {
      id: '00000000-0000-0000-0000-000000000000',
      accountName: 'Test Account',
      amount: 100,
      category: 'Test',
      description: 'Test transaction',
      date: '2026-01-01T00:00:00Z',
    });
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
