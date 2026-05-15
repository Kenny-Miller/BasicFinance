import { Component, input } from '@angular/core';
import { ListResult } from '../../api/list-result';
import { Transaction } from '../../api/transactions/transactions';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { HlmIconImports } from '@spartan-ng/helm/icon';
import { provideIcons } from '@ng-icons/core';
import { lucideReceiptText } from '@ng-icons/lucide';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { TruncatePipe } from '../../pipes/truncate-pipe';

@Component({
  selector: 'app-transactions-list',
  imports: [
    HlmItemImports,
    CurrencyPipe,
    DatePipe,
    HlmIconImports,
    HlmSeparatorImports,
    TruncatePipe,
  ],
  templateUrl: './transactions-list.html',
  styleUrl: './transactions-list.css',
  providers: [
    provideIcons({
      lucideReceiptText,
    }),
  ],
})
export class TransactionsList {
  readonly transactionsResult = input.required<ListResult<Transaction>>();
}
