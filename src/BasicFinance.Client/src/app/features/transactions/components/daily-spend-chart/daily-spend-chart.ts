import { CommonModule } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowDownCircle, lucideArrowUpCircle } from '@ng-icons/lucide';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { EChartsCoreOption } from 'echarts/types/dist/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import { DailySummaryResponse } from '../../../../core/data-access/transaction-client';

interface TooltipParam {
  seriesName: string;
  value: number | null;
  axisValue: string;
  dataIndex: number;
}

type TooltipParamWithValue = TooltipParam & { value: number };

const CHART_COLORS = {
  light: {
    current: 'rgb(217, 119, 6)',
    previous: 'rgb(253, 230, 138)',
  },
  dark: {
    current: 'rgb(251, 191, 36)',
    previous: 'rgb(120, 53, 15)',
  },
};

@Component({
  selector: 'app-daily-spend-chart',
  providers: [provideIcons({ lucideArrowUpCircle, lucideArrowDownCircle })],
  imports: [CommonModule, NgIcon, HlmCardImports, NgxEchartsDirective],
  templateUrl: './daily-spend-chart.html',
  styleUrl: './daily-spend-chart.css',
})
export class DailySpendChart {
  readonly theme = input.required<string>();
  readonly data = input.required<DailySummaryResponse>();
  readonly deltaLabel = input.required();

  readonly totalSpend = computed(() =>
    (this.data()?.currentPeriod ?? []).reduce((sum, item) => sum + item.totalSpend, 0),
  );
  readonly previousSpend = computed(() =>
    (this.data()?.previousPeriod ?? []).reduce((sum, item) => sum + item.totalSpend, 0),
  );
  readonly hasPrevious = computed(() => (this.data()?.previousPeriod ?? []).length > 0);
  readonly spendDifference = computed(() => this.totalSpend() - this.previousSpend());
  readonly isSpendIncrease = computed(() => this.spendDifference() > 0);
  readonly isSpendDecrease = computed(() => this.spendDifference() < 0);
  readonly changeClass = computed(() =>
    this.isSpendIncrease() ? 'text-red-500' : 'text-emerald-500',
  );

  readonly options = computed<EChartsCoreOption>(() => {
    const data = this.data();
    const colors = this.theme() === 'dark' ? CHART_COLORS.dark : CHART_COLORS.light;
    const current = data?.currentPeriod ?? [];
    const previous = data?.previousPeriod ?? [];
    const currency = new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    });

    return {
      tooltip: {
        trigger: 'axis',
        formatter: (params: TooltipParam[]) => {
          const rows = params
            .filter((param): param is TooltipParamWithValue => param.value != null)
            .map((param) => {
              const value = currency.format(param.value);
              const previousPoint =
                param.seriesName === 'Previous period' ? previous[param.dataIndex] : undefined;
              const date = previousPoint ? this._shortDate(previousPoint.date) : param.axisValue;
              return `<div style="color:#000000;">${param.seriesName}: ${value} (${date})</div>`;
            })
            .join('');
          return rows;
        },
      },
      grid: { top: 24, right: 16, bottom: 24, left: 16, containLabel: true },
      xAxis: {
        type: 'category',
        data: current.map((item) => this._shortDate(item.date)),
        axisLine: { show: false },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'value',
        boundaryGap: false,
        axisLabel: {
          formatter: (value: number) => currency.format(value),
        },
      },
      series: [
        ...(previous.length > 0
          ? [
              {
                name: 'Previous period',
                type: 'line' as const,
                data: previous.map((item) => item.totalSpend),
                smooth: true,
                symbol: 'none',
                lineStyle: {
                  color: colors.previous,
                  width: 1.5,
                  type: 'dashed',
                },
                areaStyle: {
                  color: {
                    type: 'linear',
                    x: 0,
                    y: 0,
                    x2: 0,
                    y2: 1,
                    colorStops: [
                      { offset: 0, color: 'rgba(217, 119, 6, 0.25)' },
                      { offset: 1, color: 'rgba(217, 119, 6, 0.03)' },
                    ],
                  },
                },
                itemStyle: { color: colors.previous },
              },
            ]
          : []),
        {
          name: 'This period',
          type: 'line' as const,
          data: current.map((item) => item.totalSpend),
          smooth: true,
          symbol: 'none',
          lineStyle: {
            color: colors.current,
            width: 2,
          },
          itemStyle: { color: colors.current },
        },
      ],
    };
  });

  private _shortDate(isoDate: string): string {
    const date = new Date(`${isoDate}T00:00:00`);
    return `${date.getMonth() + 1}/${date.getDate()}`;
  }
}
