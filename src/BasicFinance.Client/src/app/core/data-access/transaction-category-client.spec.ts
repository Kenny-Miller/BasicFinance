import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TransactionCategoryClient } from './transaction-category-client';

const TRANSACTION_CATEGORIES = [
  { id: 1, code: 'UNC', name: 'Uncategorized' },
  { id: 2, code: 'AUTO', name: 'Auto and Transport' },
  { id: 3, code: 'TAXES', name: 'Taxes' },
];

describe('TransactionCategoryClient', () => {
  let client: TransactionCategoryClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClientTesting()],
    });
    controller = TestBed.inject(HttpTestingController);
    client = TestBed.inject(TransactionCategoryClient);
  });

  it('should fetch the active transaction categories when the resource is created', async () => {
    // httpResource asserts an injection context, so the provider call is wrapped the way
    // PageService's field initializer runs it.
    const transactionCategories = TestBed.runInInjectionContext(() =>
      client.transactionCategories(),
    );

    // The httpResource effect that starts the request is scheduled when the
    // resource is created; flush effects so the request is issued before it
    // can be expected.
    TestBed.flushEffects();

    const request = controller.expectOne('api/transaction-categories/');

    expect(request.request.method).toBe('GET');

    request.flush(TRANSACTION_CATEGORIES);
    // The resource applies the response in an async continuation; let it settle first.
    await Promise.resolve();
    expect(transactionCategories.value()).toEqual(TRANSACTION_CATEGORIES);
  });

  it('should issue a new request when the resource is reloaded', async () => {
    const transactionCategories = TestBed.runInInjectionContext(() =>
      client.transactionCategories(),
    );
    TestBed.flushEffects();

    const firstRequest = controller.expectOne('api/transaction-categories/');
    firstRequest.flush([]);
    // reload() is a no-op until the resource has settled to a resolved state, which
    // happens in an async continuation after the response is flushed.
    await Promise.resolve();

    transactionCategories.reload();
    TestBed.flushEffects();

    const secondRequest = controller.expectOne('api/transaction-categories/');
    secondRequest.flush(TRANSACTION_CATEGORIES);
    await Promise.resolve();
    expect(transactionCategories.hasValue()).toBeTruthy();
    expect(transactionCategories.value()).toEqual(TRANSACTION_CATEGORIES);
  });
});
