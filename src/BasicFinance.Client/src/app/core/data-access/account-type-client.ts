import { HttpClient } from '@angular/common/http';
import { Injectable, inject, resource } from '@angular/core';

import { getApiAccountTypes } from '../api/generated/sdk.gen';

export type { AccountTypeDto as AccountType } from '../api/generated/types.gen';

@Injectable({
  providedIn: 'root',
})
export class AccountTypeClient {
  private readonly httpClient = inject(HttpClient);

  /**
   * Provides the global account types resource (active rows only). The calling
   * service stores the returned resource and exposes its value/loading/error;
   * refetches are the service's job via the resource's `reload()`.
   */
  accountTypes() {
    return resource({
      loader: async () => {
        const response = await getApiAccountTypes({
          httpClient: this.httpClient,
          throwOnError: true,
        });
        return response.data;
      },
    });
  }
}
