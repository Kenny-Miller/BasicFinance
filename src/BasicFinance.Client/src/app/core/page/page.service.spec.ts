import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { AccountType, AccountTypeClient } from '../data-access/account-type-client';
import { Institution, InstitutionClient } from '../data-access/institution-client';
import {
  TransactionCategory,
  TransactionCategoryClient,
} from '../data-access/transaction-category-client';
import { TransactionType, TransactionTypeClient } from '../data-access/transaction-type-client';
import { PageService } from './page.service';

describe('PageService', () => {
  let service: PageService;

  let institutionsValue = signal<Institution[] | null>(null);
  let institutionsHasValue = signal(false);
  let institutionsError = signal<null | Error>(null);
  let institutionsReload = vi.fn();

  let accountTypesValue = signal<AccountType[] | null>(null);
  let accountTypesHasValue = signal(false);
  let accountTypesError = signal<null | Error>(null);
  let accountTypesReload = vi.fn();

  let transactionTypesValue = signal<TransactionType[] | null>(null);
  let transactionTypesHasValue = signal(false);
  let transactionTypesError = signal<null | Error>(null);
  let transactionTypesReload = vi.fn();

  let transactionCategoriesValue = signal<TransactionCategory[] | null>(null);
  let transactionCategoriesHasValue = signal(false);
  let transactionCategoriesError = signal<null | Error>(null);
  let transactionCategoriesReload = vi.fn();

  function settleInstitutions(institutions: Institution[]) {
    institutionsValue.set(institutions);
    institutionsHasValue.set(true);
  }

  function settleReferences() {
    accountTypesValue.set([
      { id: 1, code: 'CHK', name: 'Checking', isLiability: false },
      { id: 2, code: 'CC', name: 'Credit Card', isLiability: true },
    ]);
    accountTypesHasValue.set(true);
    transactionTypesValue.set([
      { id: 1, code: 'CR', name: 'Credit' },
      { id: 2, code: 'DR', name: 'Debit' },
    ]);
    transactionTypesHasValue.set(true);
    transactionCategoriesValue.set([{ id: 1, code: 'UNC', name: 'Uncategorized' }]);
    transactionCategoriesHasValue.set(true);
  }

  beforeEach(() => {
    institutionsValue = signal(null);
    institutionsHasValue = signal(false);
    institutionsError = signal(null);
    institutionsReload = vi.fn();
    accountTypesValue = signal(null);
    accountTypesHasValue = signal(false);
    accountTypesError = signal(null);
    accountTypesReload = vi.fn();
    transactionTypesValue = signal(null);
    transactionTypesHasValue = signal(false);
    transactionTypesError = signal(null);
    transactionTypesReload = vi.fn();
    transactionCategoriesValue = signal(null);
    transactionCategoriesHasValue = signal(false);
    transactionCategoriesError = signal(null);
    transactionCategoriesReload = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        {
          provide: InstitutionClient,
          useValue: {
            myInstitutions: () => ({
              hasValue: () => institutionsHasValue(),
              value: () => institutionsValue(),
              error: () => institutionsError(),
              reload: () => {
                institutionsReload();
              },
            }),
          },
        },
        {
          provide: AccountTypeClient,
          useValue: {
            accountTypes: () => ({
              hasValue: () => accountTypesHasValue(),
              value: () => accountTypesValue(),
              error: () => accountTypesError(),
              reload: () => {
                accountTypesReload();
              },
            }),
          },
        },
        {
          provide: TransactionTypeClient,
          useValue: {
            transactionTypes: () => ({
              hasValue: () => transactionTypesHasValue(),
              value: () => transactionTypesValue(),
              error: () => transactionTypesError(),
              reload: () => {
                transactionTypesReload();
              },
            }),
          },
        },
        {
          provide: TransactionCategoryClient,
          useValue: {
            transactionCategories: () => ({
              hasValue: () => transactionCategoriesHasValue(),
              value: () => transactionCategoriesValue(),
              error: () => transactionCategoriesError(),
              reload: () => {
                transactionCategoriesReload();
              },
            }),
          },
        },
      ],
    });
    service = TestBed.inject(PageService);
  });

  it('should report loading with no data until all four resources have settled', () => {
    expect(service.loading()).toBe(true);
    expect(service.data()).toBeNull();
    expect(service.error()).toBeFalsy();
  });

  it('should keep loading while a reference list is still pending after the institutions settled', () => {
    settleInstitutions([{ id: 1, code: 'WF', name: 'Wells Fargo', logoUrl: null }]);
    accountTypesHasValue.set(true);
    transactionTypesHasValue.set(true);

    expect(service.loading()).toBe(true);

    transactionCategoriesHasValue.set(true);

    expect(service.loading()).toBe(false);
  });

  it('should expose the institutions value and stop loading once all resources have loaded', () => {
    const institutions: Institution[] = [{ id: 1, code: 'WF', name: 'Wells Fargo', logoUrl: null }];
    settleInstitutions(institutions);
    settleReferences();

    expect(service.loading()).toBe(false);
    expect(service.data()).toEqual(institutions);
  });

  it('should stop loading and surface the error when the institutions fetch errors', () => {
    institutionsError.set(new Error('no institutions'));

    expect(service.loading()).toBe(false);
    expect(service.error()).toBeTruthy();
  });

  it('should stop loading and surface the error when a reference list fetch errors', () => {
    transactionCategoriesError.set(new Error('no categories'));

    expect(service.loading()).toBe(false);
    expect(service.error()).toBeTruthy();
  });

  it('should expose the reference lists, defaulting to empty arrays before they load', () => {
    expect(service.accountTypes()).toEqual([]);
    expect(service.transactionTypes()).toEqual([]);
    expect(service.transactionCategories()).toEqual([]);

    settleReferences();

    expect(service.accountTypes()).toEqual([
      { id: 1, code: 'CHK', name: 'Checking', isLiability: false },
      { id: 2, code: 'CC', name: 'Credit Card', isLiability: true },
    ]);
    expect(service.transactionTypes()).toEqual([
      { id: 1, code: 'CR', name: 'Credit' },
      { id: 2, code: 'DR', name: 'Debit' },
    ]);
    expect(service.transactionCategories()).toEqual([{ id: 1, code: 'UNC', name: 'Uncategorized' }]);
  });

  it('should refetch all four shared resources when refetchAll is called', () => {
    service.refetchAll();

    expect(institutionsReload).toHaveBeenCalledTimes(1);
    expect(accountTypesReload).toHaveBeenCalledTimes(1);
    expect(transactionTypesReload).toHaveBeenCalledTimes(1);
    expect(transactionCategoriesReload).toHaveBeenCalledTimes(1);
  });

  it('should keep the current institutions value while a refetch is requested', () => {
    const institutions: Institution[] = [{ id: 1, code: 'WF', name: 'Wells Fargo', logoUrl: null }];
    settleInstitutions(institutions);
    settleReferences();

    service.refetchAll();

    expect(service.data()).toEqual(institutions);
    expect(institutionsReload).toHaveBeenCalledTimes(1);
  });

  it('should update the page title and subtitle', () => {
    service.setPageTitle('Home');
    service.setPageSubtitle('Your dashboard');

    expect(service.pageTitle()).toBe('Home');
    expect(service.pageSubtitle()).toBe('Your dashboard');
  });
});
