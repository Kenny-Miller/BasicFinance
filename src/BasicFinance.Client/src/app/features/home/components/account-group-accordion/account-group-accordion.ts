import { CurrencyPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { HlmAccordionImports } from '@spartan-ng/helm/accordion';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { Account } from '../../../../shared/api/accounts/account';
import { TruncatePipe } from '../../../../shared/pipes/truncate-pipe';

@Component({
  selector: 'app-account-group-accordion',
  imports: [
    HlmAccordionImports,
    CurrencyPipe,
    HlmItemImports,
    HlmSeparatorImports,
    TruncatePipe,
    NgIcon,
  ],
  templateUrl: './account-group-accordion.html',
  styleUrl: './account-group-accordion.css',
})
export class AccountGroupAccordion {
  readonly accountGroupTitle = input.required<string>();
  readonly accountGroupIcon = input.required<string>();
  readonly accountGroupData = input.required<{ totalBalance: number; accounts: Account[] }>();
}
