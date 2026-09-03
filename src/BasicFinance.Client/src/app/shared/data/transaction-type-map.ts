export interface SelectOption {
  value: string;
  label: string;
}

export const TRANSACTION_TYPE_OPTIONS: SelectOption[] = [
  { value: '', label: 'All Types' },
  { value: 'CR', label: 'Credit' },
  { value: 'DR', label: 'Debit' },
];
