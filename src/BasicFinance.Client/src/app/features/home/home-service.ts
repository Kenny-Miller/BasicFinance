import { computed, inject, Injectable, signal } from '@angular/core';
import { AccountClient, TotalBalanceBreakdown } from '../../core/data-access/account-client';
import { SpendingClient, SpendingOverTimeSummary } from '../../core/data-access/spending-client';
import { Transaction, TransactionClient } from '../../core/data-access/transaction-client';
import { PageService } from '../../core/page/page.service';
import { ACCOUNT_TYPE_CODES } from '../../shared/data/account-type-map';
import { DEFAULT_TIME_PERIOD, TimePeriod } from '../../shared/data/time-period';

export interface HomeData {
  currentPeriodTotalBalance: number;
  currentPeriodCheckingBalance: number;
  currentPeriodSavingsBalance: number;
  currentPeriodInvestmentsBalance: number;
  previousPeriodBalance: number;
  previousPeriodCheckingBalance: number;
  previousPeriodSavingsBalance: number;
  previousPeriodInvestmentsBalance: number;
  currentPeriodBreakdown: TotalBalanceBreakdown;
  spendingOverTime: SpendingOverTimeSummary;
  recentTransactions: Transaction[];
}

const EMPTY_BREAKDOWN: TotalBalanceBreakdown = { balance: 0, accountTypeBreakdowns: {} };

const EMPTY_SPENDING_OVER_TIME: SpendingOverTimeSummary = {
  currentMonthActivity: [],
  previousMonthActivity: [],
  totalMonthlySpend: 0,
  monthlySpendDifference: 0,
};

@Injectable({
  providedIn: 'root',
})
export class HomeService {
  private readonly accountClient = inject(AccountClient);
  private readonly transactionClient = inject(TransactionClient);
  private readonly spendingClient = inject(SpendingClient);
  private readonly pageService = inject(PageService);

  private readonly timePeriod = signal<TimePeriod>(DEFAULT_TIME_PERIOD);

  private readonly balanceSummaryResource = this.accountClient.balanceSummaryResource(
    this.timePeriod,
  );
  private readonly transactionsResource = this.transactionClient.listTransactions(
    signal(1),
    signal(5),
    signal('Date'),
    signal('desc'),
    signal({}),
  );

  private readonly spendingOverTimeResource = this.spendingClient.spendingOverTimeSummaryResource();

  readonly loading = computed(
    () =>
      this.pageService.loading() ||
      (!this.balanceSummaryResource.hasValue() &&
        !this.spendingOverTimeResource.hasValue() &&
        !this.transactionsResource.hasValue()),
  );

  readonly error = computed(
    () =>
      this.pageService.error() ||
      this.balanceSummaryResource.error() ||
      this.spendingOverTimeResource.error() ||
      this.transactionsResource.error(),
  );

  readonly data = computed<HomeData>(() => {
    const balanceSummary = this.balanceSummaryResource.value();
    const current = balanceSummary?.currentPeriodBreakdown;
    const previous = balanceSummary?.previousPeriodBreakdown;

    return {
      currentPeriodTotalBalance: current?.balance ?? 0,
      currentPeriodCheckingBalance: this.typeBalance(current, ACCOUNT_TYPE_CODES.CHECKING),
      currentPeriodSavingsBalance: this.typeBalance(current, ACCOUNT_TYPE_CODES.SAVINGS),
      currentPeriodInvestmentsBalance: this.typeBalance(current, ACCOUNT_TYPE_CODES.INVESTMENTS),
      previousPeriodBalance: previous?.balance ?? 0,
      previousPeriodCheckingBalance: this.typeBalance(previous, ACCOUNT_TYPE_CODES.CHECKING),
      previousPeriodSavingsBalance: this.typeBalance(previous, ACCOUNT_TYPE_CODES.SAVINGS),
      previousPeriodInvestmentsBalance: this.typeBalance(previous, ACCOUNT_TYPE_CODES.INVESTMENTS),
      currentPeriodBreakdown: current ?? EMPTY_BREAKDOWN,
      spendingOverTime: this.spendingOverTimeResource.value() ?? EMPTY_SPENDING_OVER_TIME,
      recentTransactions: this.transactionsResource.value()?.items ?? [],
    };
  });

  refetchAll(): void {
    this.pageService.refetchAll();
    this.balanceSummaryResource.reload();
    this.transactionsResource.reload();
    this.spendingOverTimeResource.reload();
  }

  private typeBalance(breakdown: TotalBalanceBreakdown | undefined, typeCode: string): number {
    return breakdown?.accountTypeBreakdowns[typeCode]?.balance ?? 0;
  }
}
