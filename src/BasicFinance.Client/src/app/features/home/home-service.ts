import { computed, inject, Injectable, signal } from '@angular/core';
import { AccountClient, TotalBalanceBreakdown } from '../../core/data-access/account-client';
import { SpendingClient } from '../../core/data-access/spending-client';
import { Transaction, TransactionClient } from '../../core/data-access/transaction-client';
import { ACCOUNT_TYPE_CODES } from '../../shared/data/account-type-map';
import { DEFAULT_TIME_PERIOD, TimePeriod } from '../../shared/data/time-period';

@Injectable({
  providedIn: 'root',
})
export class HomeService {
  private readonly accountClient = inject(AccountClient);
  private readonly transactionClient = inject(TransactionClient);
  private readonly spendingClient = inject(SpendingClient);

  readonly timePeriod = signal<TimePeriod>(DEFAULT_TIME_PERIOD);

  readonly balanceSummaryResource = this.accountClient.createBalanceSummaryResource(this.timePeriod);

  readonly transactionsResource = this.transactionClient.listTransactions(
    signal(1),
    signal(5),
    signal('Date'),
    signal('desc'),
    signal({}),
  );

  readonly spendingOverTimeResource = this.spendingClient.createSpendingOverTimeSummaryResource();

  readonly loading = computed(() =>
    !this.balanceSummaryResource.hasValue() ||
    !this.spendingOverTimeResource.hasValue() ||
    !this.transactionsResource.hasValue(),
  );

  readonly error = computed(() =>
    (this.balanceSummaryResource.error() !== undefined && !this.balanceSummaryResource.hasValue()) ||
    (this.spendingOverTimeResource.error() !== undefined && !this.spendingOverTimeResource.hasValue()) ||
    (this.transactionsResource.error() !== undefined && !this.transactionsResource.hasValue()),
  );

  readonly currentNetWorth = computed(
    () => this.balanceSummaryResource.value()?.currentPeriodBreakdown.balance ?? 0,
  );
  readonly previousNetWorth = computed(
    () => this.balanceSummaryResource.value()?.previousPeriodBreakdown.balance ?? 0,
  );

  readonly currentChecking = computed(
    () =>
      this.balanceSummaryResource.value()?.currentPeriodBreakdown.accountTypeBreakdowns[
        ACCOUNT_TYPE_CODES.CHECKING
      ]?.balance ?? 0,
  );
  readonly previousChecking = computed(
    () =>
      this.balanceSummaryResource.value()?.previousPeriodBreakdown.accountTypeBreakdowns[
        ACCOUNT_TYPE_CODES.CHECKING
      ]?.balance ?? 0,
  );

  readonly currentSavings = computed(
    () =>
      this.balanceSummaryResource.value()?.currentPeriodBreakdown.accountTypeBreakdowns[
        ACCOUNT_TYPE_CODES.SAVINGS
      ]?.balance ?? 0,
  );
  readonly previousSavings = computed(
    () =>
      this.balanceSummaryResource.value()?.previousPeriodBreakdown.accountTypeBreakdowns[
        ACCOUNT_TYPE_CODES.SAVINGS
      ]?.balance ?? 0,
  );

  readonly currentInvestments = computed(
    () =>
      this.balanceSummaryResource.value()?.currentPeriodBreakdown.accountTypeBreakdowns[
        ACCOUNT_TYPE_CODES.INVESTMENTS
      ]?.balance ?? 0,
  );
  readonly previousInvestments = computed(
    () =>
      this.balanceSummaryResource.value()?.previousPeriodBreakdown.accountTypeBreakdowns[
        ACCOUNT_TYPE_CODES.INVESTMENTS
      ]?.balance ?? 0,
  );

  readonly currentPeriodBreakdown = computed<TotalBalanceBreakdown>(
    () =>
      this.balanceSummaryResource.value()?.currentPeriodBreakdown ?? {
        balance: 0,
        accountTypeBreakdowns: {},
      },
  );

  readonly spendingOverTimeData = computed(
    () => this.spendingOverTimeResource.value(),
  );

  readonly recentTransactions = computed<Transaction[]>(
    () => this.transactionsResource.value()?.items ?? [],
  );

  refetchAll(): void {
    this.balanceSummaryResource.reload();
    this.transactionsResource.reload();
    this.spendingOverTimeResource.reload();
  }
}
