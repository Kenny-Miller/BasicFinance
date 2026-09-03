import { Component, signal } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { TransactionsSummaryTile } from './transactions-summary-tile';

interface TileOverrides {
  title?: string;
  currentValue?: number;
  lastPeriodValue?: number;
  deltaLabel?: string;
  positiveIsGood?: boolean;
  isCurrency?: boolean;
}

@Component({
  selector: 'app-test-host',
  template: ` <app-transactions-summary-tile
    [title]="title()"
    [currentValue]="currentValue()"
    [lastPeriodValue]="lastPeriodValue()"
    [deltaLabel]="deltaLabel()"
    [positiveIsGood]="positiveIsGood()"
    [isCurrency]="isCurrency()"
  /> `,
  imports: [TransactionsSummaryTile],
})
class TestHost {
  title = signal('Test Tile');
  currentValue = signal(0);
  lastPeriodValue = signal(0);
  deltaLabel = signal('last month');
  positiveIsGood = signal(true);
  isCurrency = signal(true);
}

describe('TransactionsSummaryTile', () => {
  function createTile(overrides?: TileOverrides): {
    fixture: ComponentFixture<TestHost>;
    tile: TransactionsSummaryTile;
  } {
    TestBed.configureTestingModule({ imports: [TestHost] });

    const fixture = TestBed.createComponent(TestHost);
    const host = fixture.componentInstance;
    host.title.set(overrides?.title ?? 'Test Tile');
    host.currentValue.set(overrides?.currentValue ?? 0);
    host.lastPeriodValue.set(overrides?.lastPeriodValue ?? 0);
    host.deltaLabel.set(overrides?.deltaLabel ?? 'last month');
    host.positiveIsGood.set(overrides?.positiveIsGood ?? true);
    host.isCurrency.set(overrides?.isCurrency ?? true);
    fixture.detectChanges();

    const tile = fixture.debugElement.children[0].componentInstance as TransactionsSummaryTile;

    return { fixture, tile };
  }

  describe('delta text', () => {
    it('should show the difference and percent growth for a currency value', () => {
      const { tile } = createTile({ currentValue: 1500, lastPeriodValue: 1000 });

      expect(tile.deltaText()).toBe('+$500.00 (+50.0%)');
    });

    it('should show the difference and percent decline for a currency value', () => {
      const { tile } = createTile({ currentValue: 800, lastPeriodValue: 1000 });

      expect(tile.deltaText()).toBe('-$200.00 (-20.0%)');
    });

    it('should format a non-currency value as a count with percent', () => {
      const { tile } = createTile({
        currentValue: 1300,
        lastPeriodValue: 1000,
        isCurrency: false,
      });

      expect(tile.deltaText()).toBe('+300 (+30.0%)');
    });

    it('should show the difference only when the last period was zero', () => {
      const { tile } = createTile({ currentValue: 500, lastPeriodValue: 0 });

      expect(tile.deltaText()).toBe('+$500.00');
    });

    it('should format the count difference only when the last period was zero', () => {
      const { tile } = createTile({
        currentValue: 5,
        lastPeriodValue: 0,
        isCurrency: false,
      });

      expect(tile.deltaText()).toBe('+5');
    });

    it('should say "No change" when both periods are zero', () => {
      const { tile } = createTile();

      expect(tile.deltaText()).toBe('No change');
    });
  });

  describe('favorability', () => {
    it('should treat growth as favorable for income', () => {
      const { tile } = createTile({
        currentValue: 1500,
        lastPeriodValue: 1000,
        positiveIsGood: true,
      });

      expect(tile.isFavorable()).toBe(true);
    });

    it('should treat a decrease as favorable for spending', () => {
      const { tile } = createTile({
        currentValue: 800,
        lastPeriodValue: 1000,
        positiveIsGood: false,
      });

      expect(tile.isFavorable()).toBe(true);
    });

    it('should treat a decrease as unfavorable for income', () => {
      const { tile } = createTile({
        currentValue: 1000,
        lastPeriodValue: 1200,
        positiveIsGood: true,
      });

      expect(tile.isFavorable()).toBe(false);
    });

    it('should treat an unchanged value as favorable for transaction counts', () => {
      const { tile } = createTile({
        currentValue: 100,
        lastPeriodValue: 100,
        isCurrency: false,
        positiveIsGood: false,
      });

      expect(tile.isFavorable()).toBe(true);
    });
  });

  describe('template', () => {
    it('should render a count without currency formatting', () => {
      const { fixture } = createTile({
        title: 'Transactions',
        currentValue: 1005,
        lastPeriodValue: 0,
        isCurrency: false,
      });

      const value = fixture.nativeElement.querySelector('.font-semibold') as HTMLElement;
      const title = fixture.nativeElement.querySelector('h3') as HTMLElement;

      expect(title.textContent).toContain('Transactions');
      expect(value.textContent).toBe('1005');
    });

    it('should render the difference + percent next to a neutral delta label', () => {
      const { fixture } = createTile({
        currentValue: 1500,
        lastPeriodValue: 1000,
        deltaLabel: 'last quarter',
      });

      const labelSpans = fixture.nativeElement.querySelectorAll(
        'span.text-gray-500',
      ) as HTMLElement[];
      const deltaSpans = fixture.nativeElement.querySelectorAll(
        'span.text-emerald-500',
      ) as HTMLElement[];

      expect(labelSpans.length).toBe(1);
      expect(labelSpans[0].textContent).toContain('vs last quarter');
      expect(deltaSpans.length).toBe(1);
      expect(deltaSpans[0].textContent).toBe('+$500.00 (+50.0%)');
    });

    it('should mark an unfavorable delta with red', () => {
      const { fixture } = createTile({
        currentValue: 800,
        lastPeriodValue: 1000,
        positiveIsGood: true,
      });

      expect(fixture.nativeElement.querySelector('span.text-red-500')?.textContent).toBe(
        '-$200.00 (-20.0%)',
      );
    });
  });
});
