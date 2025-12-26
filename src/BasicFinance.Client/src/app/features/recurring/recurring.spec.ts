import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Recurring } from './recurring';

describe('Recurring', () => {
  let component: Recurring;
  let fixture: ComponentFixture<Recurring>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Recurring]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Recurring);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
