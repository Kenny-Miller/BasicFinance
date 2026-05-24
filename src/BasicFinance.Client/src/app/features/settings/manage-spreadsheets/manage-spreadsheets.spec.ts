import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ManageSpreadsheets } from './manage-spreadsheets';

describe('ManageSpreadsheets', () => {
  let component: ManageSpreadsheets;
  let fixture: ComponentFixture<ManageSpreadsheets>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManageSpreadsheets],
    }).compileComponents();

    fixture = TestBed.createComponent(ManageSpreadsheets);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
