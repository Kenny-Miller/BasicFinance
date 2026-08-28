import { CommonModule } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowDownCircle, lucideArrowUpCircle } from '@ng-icons/lucide';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { EChartsCoreOption } from 'echarts/types/dist/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import { SpendingOverTimeSummary } from '../../../../core/data-access/spending-client';
import { ThemeService } from '../../../../core/theme/theme.service';

interface TooltipParam {
  seriesIndex: number;
  value: number;
}

// Hardcoded for now — plain rgb/rgba so zrender can parse them directly
// (no Tailwind CSS variables involved yet).
const CHART_COLORS = {
  light: {
    primary: 'rgb(217, 119, 6)', // amber-600 — current month
    previous: 'rgb(253, 230, 138)', // amber-200 — previous month
  },
  dark: {
    primary: 'rgb(251, 191, 36)', // amber-400 — brighter for contrast on dark
    previous: 'rgb(120, 53, 15)', // amber-900 — dim tint for a dark surface
  },
};

@Component({
  selector: 'app-spend-activity-chart',
  providers: [provideIcons({ lucideArrowUpCircle, lucideArrowDownCircle })],
  imports: [CommonModule, NgxEchartsDirective, HlmCardImports, NgIcon],
  templateUrl: './spend-activity-chart.html',
  styleUrl: './spend-activity-chart.css',
})
export class SpendActivityChart {
  private readonly themeService = inject(ThemeService);

  readonly theme = input.required<string>();
  readonly data = input<SpendingOverTimeSummary | undefined>();

  readonly totalSpend = computed(() => this.data()?.totalMonthlySpend ?? 0);
  readonly spendDifference = computed(() => Math.abs(this.data()?.monthlySpendDifference ?? 0));
  readonly isSpendIncrease = computed(() => (this.data()?.monthlySpendDifference ?? 0) >= 0);
  readonly changeClass = computed(() =>
    this.isSpendIncrease() ? 'text-red-500' : 'text-emerald-500',
  );

  readonly options = computed<EChartsCoreOption>(() => {
    const spendingData = this.data();

    const colors = this.theme() === 'dark' ? CHART_COLORS.dark : CHART_COLORS.light;
    const primaryColor = colors.primary;
    const previousMonthColor = colors.previous;

    const currentData = spendingData?.currentMonthActivity.map((item) => item.y) ?? [];
    const previousData = spendingData?.previousMonthActivity.map((item) => item.y) ?? [];

    return {
      tooltip: {
        trigger: 'axis',
        formatter: (params: TooltipParam[]) => {
          const current = params.find((p) => p.seriesIndex === 1);
          const previous = params.find((p) => p.seriesIndex === 0);
          if (!current || !previous) return '';
          const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
          let result = '';
          if (current)
            result += `<div style="color:#000000;">Current: ${currency.format(current.value)}</div>`;
          if (previous)
            result += `<div style="color:#ffffff;">Previous: ${currency.format(previous.value)}</div>`;
          return result;
        },
      },
      grid: {
        top: 0,
        right: 0,
        bottom: 0,
        left: 0,
      },
      xAxis: {
        type: 'category',
        show: true,
        axisLine: { show: false },
        axisTick: { show: false },
        axisLabel: { show: false },
      },
      yAxis: {
        type: 'value',
        boundaryGap: false,
        axisLine: { show: false },
        axisTick: { show: false },
      },
      series: [
        {
          name: 'Previous',
          data: previousData,
          type: 'line',
          smooth: true,
          symbol: 'none',
          lineStyle: {
            color: previousMonthColor,
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
                { offset: 0, color: 'rgba(217, 119, 6, 0.25)' }, // amber-600 @ 25%
                { offset: 1, color: 'rgba(217, 119, 6, 0.03)' },
              ],
            },
          },
        },
        {
          name: 'Current',
          data: currentData,
          type: 'line',
          smooth: true,
          symbol: 'none',
          lineStyle: {
            color: primaryColor,
            width: 2,
          },
        },
      ],
    };
  });
}
