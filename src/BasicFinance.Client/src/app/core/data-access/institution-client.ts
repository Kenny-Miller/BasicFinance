import { HttpClient, httpResource } from '@angular/common/http';
import { Injectable, Signal, inject } from '@angular/core';
import { ListResult } from '../../shared/api/list-result';

export interface Institution {
  id: string;
  code: string;
  name: string;
  logoUrl: string;
}

@Injectable({
  providedIn: 'root',
})
export class InstitutionClient {
  client = inject(HttpClient);

  getInstitution(institutionId: string) {
    return this.client.get<Institution>(`api/institutions/${institutionId}`);
  }

  getMyInstitutions() {
    return this.client.get<Institution>('api/my/institutions');
  }

  listInstitutions(
    pageSignal: Signal<number>,
    sortFieldSignal: Signal<string>,
    sortDirectionSignal: Signal<string>,
  ) {
    return httpResource<ListResult<Institution>>(() => {
      const params = new URLSearchParams();

      params.set('page', String(pageSignal()));
      params.set('pageSize', '20');
      params.set('sortField', sortFieldSignal());
      params.set('sortDirection', sortDirectionSignal());

      const qs = params.toString();
      return qs ? `api/institutions?${qs}` : 'api/institutions';
    });
  }
}
