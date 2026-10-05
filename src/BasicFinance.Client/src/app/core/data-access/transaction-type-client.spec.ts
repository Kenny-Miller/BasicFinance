import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
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
    // httpResource asserts an injection context, so the provider call is wrapped the way
    // PageService's field initializer runs it.
    const transactionTypes = TestBed.runInInjectionContext(() => client.transactionTypes());

    // The httpResource effect that starts the request is scheduled when the
    // resource is created; flush effects so the request is issued before it
    // can be expected.
    TestBed.flushEffects();

    const request = controller.expectOne('api/transaction-types/');

    expect(request.request.method).toBe('GET');

    request.flush(TRANSACTION_TYPES);
    // The resource applies the response in an async continuation; let it settle first.
    await Promise.resolve();
    expect(transactionTypes.value()).toEqual(TRANSACTION_TYPES);
  });

  it('should issue a new request when the resource is reloaded', async () => {
    const transactionTypes = TestBed.runInInjectionContext(() => client.transactionTypes());
    TestBed.flushEffects();

    const firstRequest = controller.expectOne('api/transaction-types/');
    firstRequest.flush([]);
    // reload() is a no-op until the resource has settled to a resolved state, which
    // happens in an async continuation after the response is flushed.
    await Promise.resolve();

    transactionTypes.reload();
    TestBed.flushEffects();

    const secondRequest = controller.expectOne('api/transaction-types/');
    secondRequest.flush(TRANSACTION_TYPES);
    await Promise.resolve();
    expect(transactionTypes.hasValue()).toBeTruthy();
    expect(transactionTypes.value()).toEqual(TRANSACTION_TYPES);
  });
});
