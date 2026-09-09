/**
 * Stable account-type identifiers used as lookup keys for the balance
 * endpoints. These are stable codes, not display data: display names come
 * from the global account-types reference list.
 */
export const ACCOUNT_TYPE_CODES = {
  CHECKING: 'CHK',
  SAVINGS: 'SAV',
  INVESTMENTS: 'INV',
  CREDIT_CARDS: 'CC',
} as const;
