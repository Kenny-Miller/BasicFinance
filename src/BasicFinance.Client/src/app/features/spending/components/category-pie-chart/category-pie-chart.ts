import { CommonModule } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { EChartsCoreOption } from 'echarts/types/dist/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import { ThemeService } from '../../../../core/theme/theme.service';
import { SpendingByPeriod } from '../../../../shared/api/spending/spending-by-period';
import { getCategoryName, SPENDING_CATEGORY_CODES } from '../../../../shared/data/category-map';

@Component({
  selector: 'app-category-pie-chart',
  imports: [CommonModule, NgxEchartsDirective, HlmCardImports],
  templateUrl: './category-pie-chart.html',
  styleUrl: './category-pie-chart.css',
})
export class CategoryPieChart {
  private readonly themeService = inject(ThemeService);

  readonly theme = input.required<string>();
  readonly data = input<SpendingByPeriod | undefined>();

  readonly pieData = computed(() => {
    const spending = this.data();
    if (!spending || !spending.spendingActivityByCategory) {
      return [];
    }

    return Object.entries(spending.spendingActivityByCategory)
      .filter(([code]) => SPENDING_CATEGORY_CODES.has(code))
      .map(([code, activity]) => ({
        name: getCategoryName(code),
        value: activity.amount,
      }))
      .filter((item) => item.value > 0)
      .sort((a, b) => b.value - a.value);
  });

  readonly options = computed<EChartsCoreOption>(() => {
    const data = this.pieData();
    return {
      tooltip: {
        trigger: 'item',
        formatter: (params: unknown) => {
          const p = params as { name: string; value: number; percent: number };
          const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
          const percent = new Intl.NumberFormat('en-US', {
            minimumFractionDigits: 1,
            maximumFractionDigits: 1,
          });
          return `${p.name}<br/>${currency.format(p.value)} (${percent.format(p.percent)}%)`;
        },
      },
      legend: {
        show: false,
      },
      series: [
        {
          type: 'pie',
          radius: ['0%', '70%'],
          avoidLabelOverlap: true,
          itemStyle: {
            borderRadius: 4,
          },
          label: {
            show: true,
            formatter: '{b}: {d}%',
            fontSize: 11,
          },
          emphasis: {
            label: {
              show: true,
              fontSize: 13,
              fontWeight: 'bold',
            },
          },
          labelLine: {
            length: 10,
            length2: 15,
          },
          data,
        },
      ],
    };
  });
}
