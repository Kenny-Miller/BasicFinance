import { SPENDING_PERIOD_PARAM, TIME_PERIOD_PARAM } from './period-params';

describe('TIME_PERIOD_PARAM', () => {
  it('maps each app time period to the server TimePeriod ordinal', () => {
    expect(TIME_PERIOD_PARAM).toEqual({
      Weekly: 1,
      Monthly: 2,
      Quarterly: 3,
      Yearly: 4,
    });
  });
});

describe('SPENDING_PERIOD_PARAM', () => {
  it('maps each app time period to the spending SpendingPeriod ordinal', () => {
    expect(SPENDING_PERIOD_PARAM).toEqual({
      Weekly: 0,
      Monthly: 1,
      Quarterly: 2,
      Yearly: 3,
    });
  });
});
