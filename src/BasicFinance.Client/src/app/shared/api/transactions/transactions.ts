export interface Transaction {
  id: string;
  transactionTypeName: string;
  transactionCategoryName: string;
  accountName: string;
  amount: number;
  description: string;
  date: string;
}
