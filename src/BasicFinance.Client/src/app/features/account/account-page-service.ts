import { computed, inject, Injectable, signal } from '@angular/core';
import {
  AccountBalanceDto,
  AccountClient,
  InstitutionSummaryResponse,
} from '../../core/data-access/account-client';
import { getAccountTypeLabel } from '../../shared/data/account-type-map';
import { DEFAULT_TIME_PERIOD, TimePeriod } from '../../shared/data/time-period';

export interface AccountTypeCard {
  code: string;
  label: string;
  balance: number;
  previousBalance: number;
}

export interface AccountPageData {
  institutionName: string;
  totalBalance: number;
  previousTotalBalance: number;
  typeCards: AccountTypeCard[];
  accounts: AccountBalanceDto[];
}

const TYPE_ORDER = ['CHK', 'SAV', 'INV', 'CC'];

const EMPTY_DATA: AccountPageData = {
  institutionName: '',
  totalBalance: 0,
  previousTotalBalance: 0,
  typeCards: [],
  accounts: [],
};

function safePercent(part: number, total: number): number {
  if (!Number.isFinite(part) || !Number.isFinite(total) || total === 0) {
    return 0;
  }
  return (part / total) * 100;
}

@Injectable({
  providedIn: 'root',
})
export class AccountPageService {
  private readonly accountClient = inject(AccountClient);

  readonly institutionId = signal<number>(0);
  readonly timePeriod = signal<TimePeriod>(DEFAULT_TIME_PERIOD);
  readonly selectedPeriod = this.timePeriod.asReadonly();

  private readonly summaryResource = this.accountClient.institutionSummaryResource(
    this.institutionId,
    this.timePeriod,
  );

  readonly loading = computed(() => !this.summaryResource.hasValue());

  readonly error = computed(() => this.summaryResource.error());

  readonly institutionName = computed(
    () => this.summaryResource.value()?.institutionName ?? '',
  );

  readonly data = computed<AccountPageData>(() => {
    const summary = this.summaryResource.value();
    if (!summary) {
      return EMPTY_DATA;
    }

    const totalBalance = summarizePeriodTotals(summary.accountTypeTotals);
    const previousTotalBalance = summarizePeriodTotals(summary.accountTypePreviousTotals);

    const presentCodes = new Set(summary.accounts.map((account) => account.accountTypeCode));
    const typeCards: AccountTypeCard[] = TYPE_ORDER.filter((code) => presentCodes.has(code)).map(
      (code) => ({
        code,
        label: getAccountTypeLabel(code),
        balance: summary.accountTypeTotals[code] ?? 0,
        previousBalance: summary.accountTypePreviousTotals[code] ?? 0,
      }),
    );

    const accounts: AccountBalanceDto[] = summary.accounts.map((account) => {
      const balance = account.latestBalance ?? 0;

      return {
        id: account.id,
        accountTypeCode: account.accountTypeCode,
        institution: summary.institutionName,
        accountName: account.name,
        balance,
        percentageOfTotalBalance: safePercent(balance, totalBalance),
        percentageOfAccountTypeBalance: safePercent(
          balance,
          summary.accountTypeTotals[account.accountTypeCode] ?? 0,
        ),
      };
    });

    return {
      institutionName: summary.institutionName,
      totalBalance,
      previousTotalBalance,
      typeCards,
      accounts,
    };
  });

  setInstitutionId(institutionId: number): void {
    this.institutionId.set(institutionId);
  }

  selectPeriod(period: TimePeriod): void {
    this.timePeriod.set(period);
  }

  refetchAll(): void {
    this.summaryResource.reload();
  }
}

function summarizePeriodTotals(totals: InstitutionSummaryResponse['accountTypeTotals']): number {
  return Object.values(totals).reduce((sum, value) => sum + value, 0);
}
