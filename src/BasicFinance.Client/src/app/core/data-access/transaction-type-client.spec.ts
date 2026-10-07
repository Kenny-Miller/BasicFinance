import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { settleSdkBridge } from '../testing/testbed-utils';
import { TransactionTypeClient } from './transaction-type-client';

const TRANSACTION_TYPES = [
  { id: 1, code: 'CR', name: 'Credit' },
  { id: 2, code: 'DR', name: 'Debit' },
];

describe('TransactionTypeClient', () => {
  let client: TransactionTypeClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClientTesting()],
    });
    controller = TestBed.inject(HttpTestingController);
    client = TestBed.inject(TransactionTypeClient);
  });

  it('should fetch the active transaction types when the resource is created', async () => {
    // resource asserts an injection context, so the provider call is wrapped the way
    // PageService's field initializer runs it.
    const transactionTypes = TestBed.runInInjectionContext(() => client.transactionTypes());
    await settleSdkBridge();

    const request = controller.expectOne('/api/transaction-types');

    expect(request.request.method).toBe('GET');

    request.flush(TRANSACTION_TYPES);
    await settleSdkBridge();
    expect(transactionTypes.value()).toEqual(TRANSACTION_TYPES);
  });

  it('should issue a new request when the resource is reloaded', async () => {
    const transactionTypes = TestBed.runInInjectionContext(() => client.transactionTypes());
    await settleSdkBridge();

    const firstRequest = controller.expectOne('/api/transaction-types');
    firstRequest.flush([]);
    await settleSdkBridge();

    transactionTypes.reload();
    await settleSdkBridge();

    const secondRequest = controller.expectOne('/api/transaction-types');
    secondRequest.flush(TRANSACTION_TYPES);
    await settleSdkBridge();
    expect(transactionTypes.hasValue()).toBeTruthy();
    expect(transactionTypes.value()).toEqual(TRANSACTION_TYPES);
  });
});
