import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';

import { AccountItem } from '../../shared/ui/accounts/account-item/account-item';
import { AppPageHeader } from '../../shared/ui/app-page-header/app-page-header';
import { SummaryCard } from '../../shared/ui/cards/summary-card/summary-card';
import { InstitutionAvatar } from '../../shared/ui/institutions/institution-avatar/institution-avatar';
import { TimePeriodSelector } from '../../shared/ui/time-period-selector/time-period-selector';
import { AccountPageService } from './account-page-service';
import { AccountSkeleton } from './components/account-skeleton/account-skeleton';

@Component({
  selector: 'app-account',
  imports: [
    AppPageHeader,
    TimePeriodSelector,
    InstitutionAvatar,
    SummaryCard,
    AccountItem,
    AccountSkeleton,
    HlmButtonImports,
    HlmCardImports,
  ],
  templateUrl: './account.html',
  styleUrl: './account.css',
})
export class Account {
  private readonly accountPageService = inject(AccountPageService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = this.accountPageService.loading;
  readonly error = this.accountPageService.error;
  readonly data = this.accountPageService.data;
  readonly institutionName = this.accountPageService.institutionName;
  readonly selectedTimePeriod = this.accountPageService.selectedTimePeriod;
  readonly selectedDate = this.accountPageService.selectedDate;

  readonly refetchAll = (): void => this.accountPageService.refetchAll();

  constructor() {
    this.route.params.subscribe(params => {
      const parsed = Number.parseInt(params['institutionId'] ?? '0', 10);
      this.accountPageService.setInstitutionId(Number.isNaN(parsed) ? 0 : parsed);
    });
  }
}
