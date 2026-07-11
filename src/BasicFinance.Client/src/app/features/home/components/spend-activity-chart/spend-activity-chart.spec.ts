import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SpendActivityChart } from './spend-activity-chart';

describe('SpendActivityChart', () => {
  let component: SpendActivityChart;
  let fixture: ComponentFixture<SpendActivityChart>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SpendActivityChart]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SpendActivityChart);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
