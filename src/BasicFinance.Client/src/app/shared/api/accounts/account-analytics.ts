export interface AccountDto {
  id: string;
  accountTypeCode: string;
  institution: string;
  accountName: string;
  balance: number;
  percentageOfTotalBalance: number;
  percentageOfAccountTypeBalance: number;
}

export interface AccountTypeBreakdown {
  balance: number;
  percentageOfTotalBalance: number;
  accounts: AccountDto[];
}

export interface TotalBalanceBreakdown {
  balance: number;
  accountTypeBreakdowns: Record<string, AccountTypeBreakdown>;
}

export interface AccountAnalyticsResponse {
  currentPeriodBreakdown: TotalBalanceBreakdown;
  previousPeriodBreakdown: TotalBalanceBreakdown;
}
