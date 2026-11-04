import { TimePeriod } from '../../shared/data/time-period';

export const TIME_PERIOD_PARAM: Record<TimePeriod, number> = {
  Weekly: 1,
  Monthly: 2,
  Quarterly: 3,
  Yearly: 4,
};

export const SPENDING_PERIOD_PARAM: Record<TimePeriod, number> = {
  Weekly: 0,
  Monthly: 1,
  Quarterly: 2,
  Yearly: 3,
};
