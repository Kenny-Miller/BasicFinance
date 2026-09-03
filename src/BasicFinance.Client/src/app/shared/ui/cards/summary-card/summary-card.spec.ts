import { Component, signal } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { SummaryCard } from './summary-card';

interface CardOverrides {
  title?: string;
  currentPeriodValue?: number;
  previousPeriodValue?: number;
}

@Component({
  selector: 'app-test-host',
  template: ` <app-summary-card
    [title]="title()"
    [currentPeriodValue]="currentPeriodValue()"
    [previousPeriodValue]="previousPeriodValue()"
  /> `,
  imports: [SummaryCard],
})
class TestHost {
  title = signal('Test Card');
  currentPeriodValue = signal(0);
  previousPeriodValue = signal(0);
}

describe('SummaryCard', () => {
  function createCard(overrides?: CardOverrides): {
    fixture: ComponentFixture<TestHost>;
    card: SummaryCard;
  } {
    TestBed.configureTestingModule({ imports: [TestHost] });

    const fixture = TestBed.createComponent(TestHost);
    const host = fixture.componentInstance;
    host.title.set(overrides?.title ?? 'Test Card');
    host.currentPeriodValue.set(overrides?.currentPeriodValue ?? 0);
    host.previousPeriodValue.set(overrides?.previousPeriodValue ?? 0);
    fixture.detectChanges();

    const card = fixture.debugElement.children[0].componentInstance as SummaryCard;

    return { fixture, card };
  }

  describe('delta text', () => {
    it('should show the percent growth when the current value is higher than the last period', () => {
      const { card } = createCard({ currentPeriodValue: 1500, previousPeriodValue: 1000 });

      expect(card.deltaText()).toBe('+50.0%');
    });

    it('should show the percent decline when the current value is lower than the last period', () => {
      const { card } = createCard({ currentPeriodValue: 800, previousPeriodValue: 1000 });

      expect(card.deltaText()).toBe('-20.0%');
    });

    it('should show the difference amount only when the last period was zero', () => {
      const { card } = createCard({ currentPeriodValue: 500, previousPeriodValue: 0 });

      expect(card.deltaText()).toBe('+$500.00');
    });

    it('should say "No change" when both periods are zero', () => {
      const { card } = createCard();

      expect(card.deltaText()).toBe('No change');
    });

    it('should keep the percent fallback when the difference is zero but the last period had a value', () => {
      const { card } = createCard({ currentPeriodValue: 1000, previousPeriodValue: 1000 });

      expect(card.deltaText()).toBe('+0.0%');
    });
  });

  describe('favorability', () => {
    it('should be favorable when the value grows', () => {
      const { card } = createCard({ currentPeriodValue: 1500, previousPeriodValue: 1000 });

      expect(card.isFavorable()).toBe(true);
    });

    it('should be unfavorable when the value shrinks', () => {
      const { card } = createCard({ currentPeriodValue: 800, previousPeriodValue: 1000 });

      expect(card.isFavorable()).toBe(false);
    });

    it('should be favorable when the value is unchanged', () => {
      const { card } = createCard({ currentPeriodValue: 1000, previousPeriodValue: 1000 });

      expect(card.isFavorable()).toBe(true);
    });
  });

  describe('template', () => {
    it('should display the title and the current value as currency', () => {
      const { fixture } = createCard({
        title: 'Total Income',
        currentPeriodValue: 1500,
        previousPeriodValue: 1000,
      });

      const title = fixture.nativeElement.querySelector('h3') as HTMLElement;
      const value = fixture.nativeElement.querySelector('.font-semibold') as HTMLElement;

      expect(title.textContent).toContain('Total Income');
      expect(value.textContent).toBe('$1,500.00');
    });

    it('should render the delta in a colored span next to a neutral "vs last month" label', () => {
      const { fixture } = createCard({ currentPeriodValue: 1500, previousPeriodValue: 1000 });

      const labelSpans = fixture.nativeElement.querySelectorAll(
        'span.text-gray-500',
      ) as HTMLElement[];
      const deltaSpans = fixture.nativeElement.querySelectorAll(
        'span.text-emerald-500',
      ) as HTMLElement[];

      expect(labelSpans.length).toBe(1);
      expect(labelSpans[0].textContent).toContain('vs last month');
      expect(deltaSpans.length).toBe(1);
      expect(deltaSpans[0].textContent).toBe('+50.0%');
    });

    it('should mark a decline with red', () => {
      const { fixture } = createCard({ currentPeriodValue: 800, previousPeriodValue: 1000 });

      expect(fixture.nativeElement.querySelector('span.text-red-500')?.textContent).toBe(
        '-20.0%',
      );
    });
  });
});
