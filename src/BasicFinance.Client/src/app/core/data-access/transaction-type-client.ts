import { HttpClient } from '@angular/common/http';
import { Injectable, inject, resource } from '@angular/core';

import { getApiTransactionTypes } from '../api/generated/sdk.gen';

export type { TransactionTypeDto as TransactionType } from '../api/generated/types.gen';

@Injectable({
  providedIn: 'root',
})
export class TransactionTypeClient {
  private readonly httpClient = inject(HttpClient);

  /**
   * Provides the global transaction types resource (active rows only). The
   * calling service stores the returned resource and exposes its
   * value/loading/error; refetches are the service's job via the resource's
   * `reload()`.
   */
  transactionTypes() {
    return resource({
      loader: async () => {
        const response = await getApiTransactionTypes({
          httpClient: this.httpClient,
          throwOnError: true,
        });
        return response.data;
      },
    });
  }
}
