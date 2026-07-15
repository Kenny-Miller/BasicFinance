import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PeriodSelector } from './period-selector';

@Component({
  selector: 'app-test-host',
  template: '<app-period-selector [activePeriod]="period()" />',
  imports: [PeriodSelector],
})
class TestHost {
  period = signal('Monthly');
}

describe('PeriodSelector', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TestHost],
    });
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(TestHost);
    fixture.detectChanges();
    const component = fixture.debugElement.children[0].componentInstance;
    expect(component).toBeTruthy();
  });

  describe('ngOnChanges', () => {
    it('should sync selectedPeriod when activePeriod changes', () => {
      const fixture = TestBed.createComponent(TestHost);
      fixture.detectChanges();
      const component = fixture.debugElement.children[0].componentInstance as PeriodSelector;
      (fixture.debugElement.componentInstance as TestHost).period.set('Quarterly');
      fixture.detectChanges();
      expect(component.selectedPeriod).toBe('Quarterly');
    });
  });
});
