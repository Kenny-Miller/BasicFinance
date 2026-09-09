import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';

export interface TransactionType {
  id: number;
  code: string;
  name: string;
}

@Injectable({
  providedIn: 'root',
})
export class TransactionTypeClient {
  /**
   * Provides the global transaction types resource (active rows only). The
   * calling service stores the returned resource and exposes its
   * value/loading/error; refetches are the service's job via the resource's
   * `reload()`.
   */
  transactionTypes() {
    return httpResource<TransactionType[]>(() => 'api/transaction-types/');
  }
}
