export const ACCOUNT_TYPE_CODES = {
  CHECKING: 'CHK',
  SAVINGS: 'SAV',
  INVESTMENTS: 'INV',
  CREDIT_CARDS: 'CC',
} as const;

export const ACCOUNT_TYPE_LABELS: Record<string, string> = {
  CHK: 'Checking',
  SAV: 'Savings',
  INV: 'Investments',
  CC: 'Credit Cards',
};

export function getAccountTypeLabel(code: string): string {
  return ACCOUNT_TYPE_LABELS[code] ?? code;
}
