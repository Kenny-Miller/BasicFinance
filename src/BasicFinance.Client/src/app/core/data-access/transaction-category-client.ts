import { HttpClient } from '@angular/common/http';
import { Injectable, inject, resource } from '@angular/core';

import { getApiTransactionCategories } from '../api/generated/sdk.gen';

export type { TransactionCategoryDto as TransactionCategory } from '../api/generated/types.gen';

@Injectable({
  providedIn: 'root',
})
export class TransactionCategoryClient {
  private readonly httpClient = inject(HttpClient);

  /**
   * Provides the global transaction categories resource (active rows only).
   * The calling service stores the returned resource and exposes its
   * value/loading/error; refetches are the service's job via the resource's
   * `reload()`.
   */
  transactionCategories() {
    return resource({
      loader: async () => {
        const response = await getApiTransactionCategories({
          httpClient: this.httpClient,
          throwOnError: true,
        });
        return response.data;
      },
    });
  }
}
