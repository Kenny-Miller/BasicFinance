import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccountGroupAccordion } from './account-group-accordion';

describe('AccountGroupAccordion', () => {
  let component: AccountGroupAccordion;
  let fixture: ComponentFixture<AccountGroupAccordion>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AccountGroupAccordion],
    }).compileComponents();

    fixture = TestBed.createComponent(AccountGroupAccordion);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
