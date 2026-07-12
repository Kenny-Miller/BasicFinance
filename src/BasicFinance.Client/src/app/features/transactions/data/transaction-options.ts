export interface SelectOption {
  value: string;
  label: string;
}

export const TRANSACTION_TYPE_OPTIONS: SelectOption[] = [
  { value: '', label: 'All Types' },
  { value: '1', label: 'Credit' },
  { value: '2', label: 'Debit' },
];

export const TRANSACTION_CATEGORY_OPTIONS: SelectOption[] = [
  { value: '', label: 'All Categories' },
  { value: '1', label: 'Uncategorized' },
  { value: '2', label: 'Auto & Transport' },
  { value: '3', label: 'Bills & Utilities' },
  { value: '4', label: 'Business' },
  { value: '5', label: 'Cash & Checks' },
  { value: '6', label: 'Charitable Donations' },
  { value: '7', label: 'Dining & Drinks' },
  { value: '8', label: 'Education' },
  { value: '9', label: 'Entertainment & Rec' },
  { value: '10', label: 'Family Care' },
  { value: '11', label: 'Fees' },
  { value: '12', label: 'Gifts' },
  { value: '13', label: 'Groceries' },
  { value: '14', label: 'Health & Wellness' },
  { value: '15', label: 'Home & Garden' },
  { value: '16', label: 'Legal' },
  { value: '17', label: 'Loan Payment' },
  { value: '18', label: 'Medical' },
  { value: '19', label: 'Personal Care' },
  { value: '20', label: 'Pets' },
  { value: '21', label: 'Shopping' },
  { value: '22', label: 'Software & Tech' },
  { value: '23', label: 'Taxes' },
  { value: '24', label: 'Travel & Vacation' },
  { value: '25', label: 'Income' },
  { value: '26', label: 'Investment' },
  { value: '27', label: 'Credit Card Payment' },
  { value: '28', label: 'Ignore' },
  { value: '29', label: 'Internal Transfer' },
  { value: '30', label: 'Reimbursement' },
  { value: '31', label: 'Savings Transfer' },
];
