import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { Transaction } from '../../../../shared/api/transactions/transactions';
import { TransactionsList } from '../../../../shared/ui/transactions/transactions-list/transactions-list';

@Component({
  selector: 'app-recent-transactions',
  imports: [HlmCardImports, HlmButtonImports, RouterLink, TransactionsList],
  templateUrl: './recent-transactions.html',
  styleUrl: './recent-transactions.css',
})
export class RecentTransactions {
  readonly transactions = input.required<Transaction[]>();
}
