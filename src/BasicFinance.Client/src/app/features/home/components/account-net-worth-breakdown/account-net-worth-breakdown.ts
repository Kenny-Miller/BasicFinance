import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { HlmAccordionImports } from '@spartan-ng/helm/accordion';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmProgressImports } from '@spartan-ng/helm/progress';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import {
  AccountTypeBreakdown,
  TotalBalanceBreakdown,
} from '../../../../shared/api/accounts/account-analytics';
import { ACCOUNT_TYPE_LABELS } from '../../../../shared/data/account-type-map';
import { AbsPipe } from '../../../../shared/pipes/abs-pipe';

interface BreakdownEntry {
  code: string;
  breakdown: AccountTypeBreakdown;
}

@Component({
  selector: 'app-account-net-worth-breakdown',
  imports: [
    HlmAccordionImports,
    HlmCardImports,
    CurrencyPipe,
    HlmSeparatorImports,
    HlmItemImports,
    HlmProgressImports,
    AbsPipe,
    DecimalPipe,
  ],
  templateUrl: './account-net-worth-breakdown.html',
  styleUrl: './account-net-worth-breakdown.css',
})
export class AccountNetWorthBreakdown {
  readonly data = input.required<TotalBalanceBreakdown>();
  readonly totalSpend = computed<number>(() => this.data().balance);
  readonly breakdownEntries = computed<BreakdownEntry[]>(() =>
    Object.entries(this.data().accountTypeBreakdowns).map(([code, breakdown]) => ({
      code,
      breakdown,
    })),
  );

  protected readonly accountTypeLabels = ACCOUNT_TYPE_LABELS;
}
