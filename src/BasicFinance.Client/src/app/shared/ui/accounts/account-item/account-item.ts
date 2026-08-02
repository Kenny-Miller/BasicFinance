import { CurrencyPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideReceiptText } from '@ng-icons/lucide';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { AccountDto } from '../../../api/accounts/account-analytics';
import { TruncatePipe } from '../../../pipes/truncate-pipe';

@Component({
  selector: 'app-account-item',
  imports: [HlmItemImports, NgIcon, CurrencyPipe, TruncatePipe],
  providers: [provideIcons({ lucideReceiptText })],
  templateUrl: './account-item.html',
  styleUrl: './account-item.css',
})
export class AccountItem {
  readonly account = input.required<AccountDto>();
}
