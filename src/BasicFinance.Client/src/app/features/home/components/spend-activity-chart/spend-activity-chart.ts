import { CommonModule } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { EChartsCoreOption } from 'echarts/types/dist/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import { ThemeService } from '../../../../core/theme/theme.service';
import { SpendingOverTimeSummary } from '../../../../shared/api/spending/spending-over-time-summary';

@Component({
  selector: 'app-spend-activity-chart',
  imports: [CommonModule, NgxEchartsDirective],
  templateUrl: './spend-activity-chart.html',
  styleUrl: './spend-activity-chart.css',
})
export class SpendActivityChart {
  private readonly themeService = inject(ThemeService);

  readonly theme = input.required<string>();
  readonly data = input<SpendingOverTimeSummary | undefined>();

  readonly options = computed<EChartsCoreOption>(() => {
    const spendingData = this.data();

    const primaryColor = '#22c55e';
    const previousMonthColor = '#000000';

    const currentData = spendingData?.currentMonthActivity.map((item) => item.y) ?? [];
    const previousData = spendingData?.previousMonthActivity.map((item) => item.y) ?? [];

    return {
      grid: {
        top: 10,
        right: 10,
        bottom: 20,
        left: 40,
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
        axisLine: { show: false },
        axisTick: { show: false },
        splitLine: {
          lineStyle: {
            type: 'dashed',
          },
        },
      },
      series: [
        {
          data: currentData,
          type: 'line',
          smooth: true,
          symbol: 'none',
          lineStyle: {
            color: primaryColor,
            width: 2,
          },
          areaStyle: {
            color: {
              type: 'linear',
              x: 0,
              y: 0,
              x2: 0,
              y2: 1,
              colorStops: [
                { offset: 0, color: primaryColor },
                { offset: 1, color: primaryColor },
              ],
            },
          },
        },
        {
          data: previousData,
          type: 'line',
          smooth: true,
          symbol: 'none',
          lineStyle: {
            color: previousMonthColor,
            width: 1.5,
            type: 'dashed',
          },
        },
      ],
    };
  });
}
