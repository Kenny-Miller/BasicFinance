import { Account } from './account';

export interface AccountTypeGroup {
  accountTypeCode: string;
  totalBalance: number;
  accounts: Account[];
}
