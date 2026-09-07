import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LineChart } from 'echarts/charts';
import { GridComponent, TooltipComponent } from 'echarts/components';
import * as echarts from 'echarts/core';
import { CanvasRenderer } from 'echarts/renderers';
import { provideEchartsCore } from 'ngx-echarts';
import {
  DailySummaryResponse,
  DailyTransactionPoint,
} from '../../../../core/data-access/transaction-client';
import { DailySpendChart } from './daily-spend-chart';

echarts.use([LineChart, GridComponent, TooltipComponent, CanvasRenderer]);

interface ChartSeries {
  name: string;
  data: number[];
  areaStyle?: {
    color: {
      type: string;
      x: number;
      y: number;
      x2: number;
      y2: number;
      colorStops: { offset: number; color: string }[];
    };
  };
}

interface ChartView {
  xAxis: { data: string[] };
  yAxis: { type: string };
  series: ChartSeries[];
  tooltip: {
    formatter: (
      params: { seriesName: string; value: number | null; axisValue: string; dataIndex: number }[],
    ) => string;
  };
}

function makePoint(
  date: string,
  totalSpend: number,
  transactionCount: number,
): DailyTransactionPoint {
  return { date, totalSpend, transactionCount };
}

function makeData(previousPeriod: DailyTransactionPoint[], currentPeriod: DailyTransactionPoint[]) {
  return {
    currentStart: '2026-08-01',
    currentEnd: '2026-08-31',
    previousStart: '2026-07-01',
    previousEnd: '2026-07-31',
    currentPeriod,
    previousPeriod,
  } as DailySummaryResponse;
}

describe('DailySpendChart', () => {
  let fixture: ComponentFixture<DailySpendChart>;
  let component: DailySpendChart;

  const previous = [
    makePoint('2026-07-01', 100, 3),
    makePoint('2026-07-02', 200, 4),
    makePoint('2026-07-03', 300, 5),
  ];
  const current = [
    makePoint('2026-08-01', 10, 1),
    makePoint('2026-08-02', 20, 2),
    makePoint('2026-08-03', 30, 3),
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DailySpendChart],
      providers: [provideEchartsCore({ echarts })],
    }).compileComponents();

    fixture = TestBed.createComponent(DailySpendChart);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('theme', 'light');
    fixture.componentRef.setInput('deltaLabel', 'last month');
    fixture.componentRef.setInput('data', makeData(previous, current));
    await fixture.whenStable();
  });

  const view = (): ChartView => component.options() as unknown as ChartView;
  const seriesNamed = (name: string): ChartSeries => {
    const series = view().series.find((item) => item.name === name);
    expect(series).toBeTruthy();
    return series as ChartSeries;
  };
  const root = (): HTMLElement => fixture.debugElement.nativeElement;
  const headerValue = (): string =>
    (root().querySelector('.text-2xl')?.textContent ?? '').replace(/\s+/g, ' ').trim();

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('plots current and previous period spend as lines on the current axis', () => {
    const options = view();

    expect(options.xAxis.data).toEqual(['8/1', '8/2', '8/3']);
    expect(options.yAxis.type).toBe('value');
    expect(options.series.map((series) => series.name)).toEqual(['Previous period', 'This period']);
    expect(seriesNamed('This period').data).toEqual([10, 20, 30]);
    expect(seriesNamed('Previous period').data).toEqual([100, 200, 300]);
  });

  it('leaves the overlay partial when the previous period is shorter', () => {
    fixture.componentRef.setInput('data', makeData([makePoint('2026-06-30', 50, 2)], current));

    expect(view().xAxis.data).toEqual(['8/1', '8/2', '8/3']);
    expect(seriesNamed('Previous period').data).toEqual([50]);
    expect(seriesNamed('This period').data).toEqual([10, 20, 30]);
  });

  it('shows no previous period series when previous period data is empty', () => {
    fixture.componentRef.setInput('data', makeData([], current));

    expect(view().xAxis.data).toEqual(['8/1', '8/2', '8/3']);
    expect(view().series.map((series) => series.name)).toEqual(['This period']);
    expect(seriesNamed('This period').data).toEqual([10, 20, 30]);
  });

  it('labels the previous period with its own date in the tooltip', () => {
    const html = view().tooltip.formatter([
      { seriesName: 'This period', value: 20, axisValue: '8/2', dataIndex: 1 },
      { seriesName: 'Previous period', value: 200, axisValue: '8/2', dataIndex: 1 },
    ]);

    expect(html).toContain('This period: $20.00 (8/2)');
    expect(html).toContain('Previous period: $200.00 (7/2)');
  });

  it('renders the total spend for the current period', () => {
    fixture.detectChanges();

    expect(headerValue()).toBe('$60.00 spending');
  });

  it('shows the spend increase above the previous period', () => {
    fixture.componentRef.setInput(
      'data',
      makeData(
        [
          makePoint('2026-07-01', 10, 1),
          makePoint('2026-07-02', 10, 1),
          makePoint('2026-07-03', 10, 1),
        ],
        [
          makePoint('2026-08-01', 20, 2),
          makePoint('2026-08-02', 20, 2),
          makePoint('2026-08-03', 20, 2),
        ],
      ),
    );
    fixture.detectChanges();

    expect(root().textContent).toContain('$30.00 above last month');
    expect(root().querySelector('.text-red-500')).toBeTruthy();
  });

  it('keeps the delta label neutral on spend changes', () => {
    fixture.componentRef.setInput(
      'data',
      makeData(
        [
          makePoint('2026-07-01', 10, 1),
          makePoint('2026-07-02', 10, 1),
          makePoint('2026-07-03', 10, 1),
        ],
        [
          makePoint('2026-08-01', 20, 2),
          makePoint('2026-08-02', 20, 2),
          makePoint('2026-08-03', 20, 2),
        ],
      ),
    );
    fixture.detectChanges();

    const colored = root().querySelector('span.text-red-500');
    expect(colored?.textContent?.trim()).toBe('$30.00');
    const label = colored?.nextElementSibling as HTMLElement | null;
    expect(label?.textContent?.trim()).toBe('above last month');
    expect(label?.className).toContain('text-gray-500');
    expect(label?.className).not.toContain('text-red-500');
  });

  it('shows the spend decrease below the previous period', () => {
    fixture.detectChanges();

    expect(root().textContent).toContain('$540.00 below last month');
    expect(root().querySelector('.text-emerald-500')).toBeTruthy();
  });

  it('shows no change when the spend is equal', () => {
    fixture.componentRef.setInput(
      'data',
      makeData(
        [
          makePoint('2026-07-01', 20, 1),
          makePoint('2026-07-02', 20, 1),
          makePoint('2026-07-03', 20, 1),
        ],
        current,
      ),
    );
    fixture.detectChanges();

    expect(root().textContent).toContain('No change vs last month');
  });

  it('fills the previous period line with the home-style gradient', () => {
    const areaStyle = seriesNamed('Previous period').areaStyle;

    expect(areaStyle?.color.type).toBe('linear');
    expect(areaStyle?.color.colorStops).toEqual([
      { offset: 0, color: 'rgba(217, 119, 6, 0.25)' },
      { offset: 1, color: 'rgba(217, 119, 6, 0.03)' },
    ]);
  });

  it('hides the delta when previous period data is empty', () => {
    fixture.componentRef.setInput('data', makeData([], current));
    fixture.detectChanges();

    expect(root().textContent).not.toContain('above last month');
    expect(root().textContent).not.toContain('below last month');
    expect(root().textContent).not.toContain('No change');
    expect(headerValue()).toBe('$60.00 spending');
  });

  it('uses the provided delta label', () => {
    fixture.componentRef.setInput('deltaLabel', 'last week');
    fixture.detectChanges();

    expect(root().textContent).toContain('below last week');
  });
});
