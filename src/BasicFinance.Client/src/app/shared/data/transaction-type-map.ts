export interface SelectOption {
  value: string;
  label: string;
}

export const TRANSACTION_TYPE_OPTIONS: SelectOption[] = [
  { value: '', label: 'All Types' },
  { value: '1', label: 'Credit' },
  { value: '2', label: 'Debit' },
];
