import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TransactionsListSkeleton } from './transactions-list-skeleton';

describe('TransactionsListSkeleton', () => {
  let component: TransactionsListSkeleton;
  let fixture: ComponentFixture<TransactionsListSkeleton>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TransactionsListSkeleton]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TransactionsListSkeleton);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
