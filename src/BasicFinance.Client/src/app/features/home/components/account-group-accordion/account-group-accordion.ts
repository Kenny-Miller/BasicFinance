import { Component, input } from '@angular/core';
import { HlmAccordionImports } from '@spartan-ng/helm/accordion';
import { HlmIconImports } from '@spartan-ng/helm/icon';
import { CurrencyPipe } from '@angular/common';
import { Account } from '../../../../shared/api/accounts/account';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { TruncatePipe } from '../../../../shared/pipes/truncate-pipe';

@Component({
  selector: 'app-account-group-accordion',
  imports: [
    HlmAccordionImports,
    HlmIconImports,
    CurrencyPipe,
    HlmItemImports,
    HlmSeparatorImports,
    TruncatePipe,
  ],
  templateUrl: './account-group-accordion.html',
  styleUrl: './account-group-accordion.css',
})
export class AccountGroupAccordion {
  readonly accountGroupTitle = input.required<string>();
  readonly accountGroupIcon = input.required<string>();
  readonly accountGroupData = input.required<{ totalBalance: number; accounts: Account[] }>();
}
