import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TransactionItemSkeleton } from './transaction-item-skeleton';

describe('TransactionItemSkeleton', () => {
  let component: TransactionItemSkeleton;
  let fixture: ComponentFixture<TransactionItemSkeleton>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TransactionItemSkeleton],
    }).compileComponents();

    fixture = TestBed.createComponent(TransactionItemSkeleton);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
