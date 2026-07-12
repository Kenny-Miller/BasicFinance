import { Component, input } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmIconImports } from '@spartan-ng/helm/icon';
import { provideIcons } from '@ng-icons/core';
import { lucideReceiptText } from '@ng-icons/lucide';
import { Transaction } from '../../../../shared/api/transactions/transactions';
import { TruncatePipe } from '../../../../shared/pipes/truncate-pipe';

@Component({
  selector: 'app-transaction-card',
  imports: [
    HlmItemImports,
    CurrencyPipe,
    DatePipe,
    HlmIconImports,
    TruncatePipe,
  ],
  templateUrl: './transaction-card.html',
  styleUrl: './transaction-card.css',
  providers: [
    provideIcons({
      lucideReceiptText,
    }),
  ],
})
export class TransactionCard {
  readonly transaction = input.required<Transaction>();
}
