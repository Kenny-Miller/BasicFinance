import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideReceiptText } from '@ng-icons/lucide';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { Transaction } from '../../../api/transactions/transactions';
import { TruncatePipe } from '../../../pipes/truncate-pipe';

@Component({
  selector: 'app-transaction-item',
  imports: [HlmItemImports, NgIcon, CurrencyPipe, DatePipe, TruncatePipe],
  providers: [provideIcons({ lucideReceiptText })],
  templateUrl: './transaction-item.html',
  styleUrl: './transaction-item.css',
})
export class TransactionItem {
  readonly transaction = input.required<Transaction>();
}
