import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';

export interface TransactionCategory {
  id: number;
  code: string;
  name: string;
}

@Injectable({
  providedIn: 'root',
})
export class TransactionCategoryClient {
  /**
   * Provides the global transaction categories resource (active rows only).
   * The calling service stores the returned resource and exposes its
   * value/loading/error; refetches are the service's job via the resource's
   * `reload()`.
   */
  transactionCategories() {
    return httpResource<TransactionCategory[]>(() => 'api/transaction-categories/');
  }
}
