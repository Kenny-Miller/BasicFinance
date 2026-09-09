/**
 * The transaction-category codes that count toward the spending breakdown.
 * This is a business rule (which categories are "spend"), not display data,
 * so it stays hardcoded; the names shown for these codes come from the
 * global transaction-categories reference list.
 */
export const SPENDING_CATEGORY_CODES = new Set([
  'UNC',
  'AUTO',
  'BILLS',
  'BUSINESS',
  'CASH',
  'DONATIONS',
  'DINING',
  'EDUCATION',
  'ENTERTAINMENT',
  'FAMILY',
  'FEES',
  'GIFTS',
  'GROCERIES',
  'HEALTH',
  'HOME',
  'LEGAL',
  'LOAN',
  'MEDICAL',
  'PERSONAL',
  'PETS',
  'SHOPPING',
  'SOFTWARE',
  'TAXES',
  'TRAVEL',
]);
