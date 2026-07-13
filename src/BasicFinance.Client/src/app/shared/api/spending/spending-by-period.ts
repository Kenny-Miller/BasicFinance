export interface SpendingActivity {
  amount: number;
  percentOfSpend: number;
}

export interface SpendingByPeriod {
  periodStartDate: string;
  periodEndDate: string;
  totalSpend: number;
  totalIncome: number;
  spendingActivityByCategory: Record<string, SpendingActivity>;
}
