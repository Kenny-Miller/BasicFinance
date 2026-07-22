import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideChartNoAxesCombined,
  lucideCreditCard,
  lucideDollarSign,
  lucideLandmark,
} from '@ng-icons/lucide';
import { HlmAccordionImports } from '@spartan-ng/helm/accordion';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { EChartsCoreOption } from 'echarts/types/dist/core';
import { NgxEchartsDirective } from 'ngx-echarts';
import {
  AccountTypeBreakdown,
  TotalBalanceBreakdown,
} from '../../../../shared/api/accounts/account-analytics';
import { AbsPipe } from '../../../../shared/pipes/abs-pipe';
import { AccountItem } from '../../../../shared/ui/accounts/account-item/account-item';

interface BreakdownEntry {
  code: string;
  breakdown: AccountTypeBreakdown;
}

interface PieData {
  name: string;
  value: number;
  itemStyle: { color: string };
}

const ACCOUNT_TYPE_COLORS: Record<string, string> = {
  CHK: '#3b82f6',
  SAV: '#10b981',
  INV: '#8b5cf6',
  CRD: '#ef4444',
};

const ACCOUNT_TYPE_LABELS: Record<string, string> = {
  CHK: 'Checking',
  SAV: 'Savings',
  INV: 'Investments',
  CRD: 'Credit Cards',
};

const ACCOUNT_TYPE_ICONS: Record<string, string> = {
  CHK: 'lucideDollarSign',
  SAV: 'lucideLandmark',
  INV: 'lucideChartNoAxesCombined',
  CRD: 'lucideCreditCard',
};

@Component({
  selector: 'app-account-net-worth-breakdown',
  providers: [
    provideIcons({
      lucideChartNoAxesCombined,
      lucideCreditCard,
      lucideDollarSign,
      lucideLandmark,
    }),
  ],
  imports: [
    HlmAccordionImports,
    HlmCardImports,
    CurrencyPipe,
    HlmSeparatorImports,
    HlmItemImports,
    AccountItem,
    AbsPipe,
    NgIcon,
    NgxEchartsDirective,
    DecimalPipe,
  ],
  templateUrl: './account-net-worth-breakdown.html',
  styleUrl: './account-net-worth-breakdown.css',
})
export class AccountNetWorthBreakdown {
  readonly data = input.required<TotalBalanceBreakdown>();

  readonly breakdownEntries = computed<BreakdownEntry[]>(() =>
    Object.entries(this.data().accountTypeBreakdowns).map(([code, breakdown]) => ({
      code,
      breakdown,
    })),
  );

  readonly pieOptions = computed<EChartsCoreOption>(() => {
    const pieData: PieData[] = this.breakdownEntries()
      .filter((e) => e.breakdown.balance > 0)
      .map((e) => ({
        name: ACCOUNT_TYPE_LABELS[e.code] ?? e.code,
        value: Math.abs(e.breakdown.percentageOfTotalBalance),
        itemStyle: { color: ACCOUNT_TYPE_COLORS[e.code] ?? '#6b7280' },
      }));

    return {
      graphic: {
        type: 'text',
        left: 'center', // Centers horizontally
        top: 'center', // Centers vertically
        style: {
          text: `${this.data().balance.toLocaleString('en-US', { style: 'currency', currency: 'USD' })}`,
          textAlign: 'center',
          fill: '#333',
          fontSize: 18,
        },
      },
      tooltip: {
        trigger: 'item',
        formatter: '{b}: {d}%',
      },
      legend: {
        show: false,
      },
      series: [
        {
          type: 'pie',
          radius: ['55%', '70%'],
          avoidLabelOverlap: true,
          padAngle: 2,
          itemStyle: {
            borderRadius: 4,
          },
          label: {
            show: false,
          },
          labelLine: {
            show: false,
          },
          data: pieData,
        },
      ],
    };
  });

  protected readonly accountTypeLabels = ACCOUNT_TYPE_LABELS;
  protected readonly accountTypeIcons = ACCOUNT_TYPE_ICONS;
  protected readonly accountTypeColors = ACCOUNT_TYPE_COLORS;
}
