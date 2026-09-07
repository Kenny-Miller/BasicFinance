import { CATEGORY_CODE_TO_NAME } from '../../../shared/data/category-map';
import { SelectOption } from '../../../shared/data/transaction-type-map';

export const TRANSACTION_CATEGORY_OPTIONS: SelectOption[] = [
  { value: '', label: 'All Categories' },
  ...Object.entries(CATEGORY_CODE_TO_NAME).map(([value, label]) => ({ value, label })),
];
