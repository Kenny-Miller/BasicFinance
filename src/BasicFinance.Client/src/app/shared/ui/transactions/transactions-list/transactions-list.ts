import { Component, input } from '@angular/core';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { Transaction } from '../../../api/transactions/transactions';
import { TransactionItem } from '../transaction-item/transaction-item';

@Component({
  selector: 'app-transactions-list',
  imports: [HlmItemImports, HlmSeparatorImports, TransactionItem],
  templateUrl: './transactions-list.html',
  styleUrl: './transactions-list.css',
})
export class TransactionsList {
  readonly transactions = input.required<Transaction[]>();
}
