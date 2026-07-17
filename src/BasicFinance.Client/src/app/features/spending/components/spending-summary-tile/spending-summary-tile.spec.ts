import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SpendingSummaryTile } from './spending-summary-tile';

describe('SpendingSummaryTile', () => {
  let component: SpendingSummaryTile;
  let fixture: ComponentFixture<SpendingSummaryTile>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SpendingSummaryTile],
    }).compileComponents();

    fixture = TestBed.createComponent(SpendingSummaryTile);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('totalIncome', 5000);
    fixture.componentRef.setInput('totalSpend', 3000);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
