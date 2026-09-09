import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AccountTypeClient } from './account-type-client';

const ACCOUNT_TYPES = [
  { id: 1, code: 'CHK', name: 'Checking', isLiability: false },
  { id: 2, code: 'SAV', name: 'Savings', isLiability: false },
  { id: 3, code: 'INV', name: 'Investment', isLiability: false },
  { id: 4, code: 'CC', name: 'Credit Card', isLiability: true },
];

describe('AccountTypeClient', () => {
  let client: AccountTypeClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClientTesting()],
    });
    controller = TestBed.inject(HttpTestingController);
    client = TestBed.inject(AccountTypeClient);
  });

  it('should fetch the active account types when the resource is created', async () => {
    // httpResource asserts an injection context, so the provider call is wrapped the way
    // PageService's field initializer runs it.
    const accountTypes = TestBed.runInInjectionContext(() => client.accountTypes());

    // The httpResource effect that starts the request is scheduled when the
    // resource is created; flush effects so the request is issued before it
    // can be expected.
    TestBed.flushEffects();

    const request = controller.expectOne('api/account-types/');

    expect(request.request.method).toBe('GET');

    request.flush(ACCOUNT_TYPES);
    // The resource applies the response in an async continuation; let it settle first.
    await Promise.resolve();
    expect(accountTypes.value()).toEqual(ACCOUNT_TYPES);
  });

  it('should issue a new request when the resource is reloaded', async () => {
    const accountTypes = TestBed.runInInjectionContext(() => client.accountTypes());
    TestBed.flushEffects();

    const firstRequest = controller.expectOne('api/account-types/');
    firstRequest.flush([]);
    // reload() is a no-op until the resource has settled to a resolved state, which
    // happens in an async continuation after the response is flushed.
    await Promise.resolve();

    accountTypes.reload();
    TestBed.flushEffects();

    const secondRequest = controller.expectOne('api/account-types/');
    secondRequest.flush(ACCOUNT_TYPES);
    await Promise.resolve();
    expect(accountTypes.hasValue()).toBeTruthy();
    expect(accountTypes.value()).toEqual(ACCOUNT_TYPES);
  });
});
