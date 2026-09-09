import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';

export interface AccountType {
  id: number;
  code: string;
  name: string;
  isLiability: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class AccountTypeClient {
  /**
   * Provides the global account types resource (active rows only). The calling
   * service stores the returned resource and exposes its value/loading/error;
   * refetches are the service's job via the resource's `reload()`.
   */
  accountTypes() {
    return httpResource<AccountType[]>(() => 'api/account-types/');
  }
}
