import { Component, input } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideReceiptText } from '@ng-icons/lucide';
import { TruncatePipe } from '../../../pipes/truncate-pipe';
import { Transaction } from '../../../api/transactions/transactions';

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
