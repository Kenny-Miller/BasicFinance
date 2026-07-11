export interface DailySpendingOverTime {
  x: number;
  y: number;
}

export interface SpendingOverTimeSummary {
  currentMonthActivity: DailySpendingOverTime[];
  previousMonthActivity: DailySpendingOverTime[];
  totalMonthlySpend: number;
  monthlySpendDifference: number;
}
