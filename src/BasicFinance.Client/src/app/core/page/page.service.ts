import { computed, Injectable, inject, signal } from '@angular/core';
import { AccountType, AccountTypeClient } from '../data-access/account-type-client';
import { Institution, InstitutionClient } from '../data-access/institution-client';
import { TransactionCategory, TransactionCategoryClient } from '../data-access/transaction-category-client';
import { TransactionType, TransactionTypeClient } from '../data-access/transaction-type-client';

@Injectable({
  providedIn: 'root',
})
export class PageService {
  private readonly _institutionClient = inject(InstitutionClient);
  private readonly _accountTypeClient = inject(AccountTypeClient);
  private readonly _transactionTypeClient = inject(TransactionTypeClient);
  private readonly _transactionCategoryClient = inject(TransactionCategoryClient);
  private readonly _pageTitle = signal<string>('');
  private readonly _pageSubtitle = signal<string>('');

  private readonly institutionsResource = this._institutionClient.myInstitutions();
  private readonly accountTypesResource = this._accountTypeClient.accountTypes();
  private readonly transactionTypesResource = this._transactionTypeClient.transactionTypes();
  private readonly transactionCategoriesResource = this._transactionCategoryClient.transactionCategories();

  readonly pageTitle = this._pageTitle.asReadonly();
  readonly pageSubtitle = this._pageSubtitle.asReadonly();

  /**
   * Page-level ("global") state shared across features. Holds the user's
   * active institutions plus the three global reference lists (account types,
   * transaction types, transaction categories), all fetched post-authentication
   * and shared across the app. Every resource gates the shared
   * loading/error contract: a page stays in its loading skeleton until all of
   * them have settled, and any failure puts the page into its error state.
   */
  readonly data = computed<Institution[] | null>(
    () => this.institutionsResource.value() ?? null,
  );

  /** The global account types (active rows only). */
  readonly accountTypes = computed<AccountType[]>(
    () => this.accountTypesResource.value() ?? [],
  );

  /** The global transaction types (active rows only). */
  readonly transactionTypes = computed<TransactionType[]>(
    () => this.transactionTypesResource.value() ?? [],
  );

  /** The global transaction categories (active rows only). */
  readonly transactionCategories = computed<TransactionCategory[]>(
    () => this.transactionCategoriesResource.value() ?? [],
  );

  /** The first resource error (institutions first), if any. */
  readonly error = computed(
    () =>
      this.institutionsResource.error() ??
      this.accountTypesResource.error() ??
      this.transactionTypesResource.error() ??
      this.transactionCategoriesResource.error(),
  );

  /** True until all page-level state has settled (loaded or errored). */
  readonly loading = computed(
    () =>
      !(
        this.institutionsResource.hasValue() &&
        this.accountTypesResource.hasValue() &&
        this.transactionTypesResource.hasValue() &&
        this.transactionCategoriesResource.hasValue()
      ) &&
      this.error() === null,
  );

  /**
   * Refetches all page-level state. Intended for use after an action that
   * updates it (for example, an action that changes the user's institutions).
   */
  refetchAll(): void {
    this.institutionsResource.reload();
    this.accountTypesResource.reload();
    this.transactionTypesResource.reload();
    this.transactionCategoriesResource.reload();
  }

  setPageTitle(title: string) {
    this._pageTitle.set(title);
  }

  setPageSubtitle(subtitle: string) {
    this._pageSubtitle.set(subtitle);
  }
}
