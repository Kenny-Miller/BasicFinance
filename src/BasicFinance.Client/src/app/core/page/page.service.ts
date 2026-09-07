import { computed, Injectable, inject, signal } from '@angular/core';
import { Institution, InstitutionClient } from '../data-access/institution-client';

@Injectable({
  providedIn: 'root',
})
export class PageService {
  private readonly _institutionClient = inject(InstitutionClient);
  private readonly _pageTitle = signal<string>('');
  private readonly _pageSubtitle = signal<string>('');

  readonly pageTitle = this._pageTitle.asReadonly();
  readonly pageSubtitle = this._pageSubtitle.asReadonly();

  /**
   * Page-level ("global") state shared across features. Currently holds the
   * user's active institutions, fetched post-authentication and shared across
   * the app; designed to be extended with other cross-feature state later
   * without reshaping the loading/error/data contract.
   */
  readonly data = computed<Institution[] | null>(
    () => this._institutionClient.myInstitutions.value() ?? null,
  );

  /** True until the global state has settled (loaded or errored). */
  readonly loading = computed(
    () =>
      !this._institutionClient.myInstitutions.hasValue() &&
      this._institutionClient.myInstitutions.error() === null,
  );

  readonly error = computed(() => this._institutionClient.myInstitutions.error());

  /**
   * Refetches all page-level state. Intended for use after an action that
   * updates it (for example, an action that changes the user's institutions).
   */
  refetchAll(): void {
    this._institutionClient.refetchMyInstitutions();
  }

  setPageTitle(title: string) {
    this._pageTitle.set(title);
  }

  setPageSubtitle(subtitle: string) {
    this._pageSubtitle.set(subtitle);
  }
}
