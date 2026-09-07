import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { TimePeriod } from '../../data/time-period';
import { PeriodSelector } from './period-selector';

@Component({
  selector: 'app-test-host',
  template: '<app-period-selector [activePeriod]="period()" [periodLabel]="label()" />',
  imports: [PeriodSelector],
})
class TestHost {
  period = signal<TimePeriod>('Monthly');
  label = signal('');
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

  describe('labeled navigation mode', () => {
    it('should emit navigate when chevron buttons are clicked', () => {
      const fixture = TestBed.createComponent(TestHost);
      fixture.detectChanges();
      const host = fixture.debugElement.componentInstance as TestHost;
      host.label.set('August 2026');
      fixture.detectChanges();

      const component = fixture.debugElement.children[0].componentInstance as PeriodSelector;
      const events: string[] = [];
      vi.spyOn(component.navigate, 'emit').mockImplementation((event: unknown) => {
        events.push(event as string);
      });

      const buttons = fixture.debugElement.nativeElement.querySelectorAll('button');
      buttons[0].click();
      buttons[1].click();

      expect(events).toEqual(['previous', 'next']);
    });
  });
});
