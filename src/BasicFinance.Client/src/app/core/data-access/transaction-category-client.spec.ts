import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { settleSdkBridge } from '../testing/testbed-utils';
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
    // resource asserts an injection context, so the provider call is wrapped the way
    // PageService's field initializer runs it.
    const transactionCategories = TestBed.runInInjectionContext(() =>
      client.transactionCategories()
    );
    await settleSdkBridge();

    const request = controller.expectOne('/api/transaction-categories');

    expect(request.request.method).toBe('GET');

    request.flush(TRANSACTION_CATEGORIES);
    await settleSdkBridge();
    expect(transactionCategories.value()).toEqual(TRANSACTION_CATEGORIES);
  });

  it('should issue a new request when the resource is reloaded', async () => {
    const transactionCategories = TestBed.runInInjectionContext(() =>
      client.transactionCategories()
    );
    await settleSdkBridge();

    const firstRequest = controller.expectOne('/api/transaction-categories');
    firstRequest.flush([]);
    await settleSdkBridge();

    transactionCategories.reload();
    await settleSdkBridge();

    const secondRequest = controller.expectOne('/api/transaction-categories');
    secondRequest.flush(TRANSACTION_CATEGORIES);
    await settleSdkBridge();
    expect(transactionCategories.hasValue()).toBeTruthy();
    expect(transactionCategories.value()).toEqual(TRANSACTION_CATEGORIES);
  });
});
