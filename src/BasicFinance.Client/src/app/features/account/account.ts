import { Component, effect, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { PageService } from '../../core/page/page.service';
import { TimePeriod } from '../../shared/data/time-period';
import { AccountItem } from '../../shared/ui/accounts/account-item/account-item';
import { SummaryCard } from '../../shared/ui/cards/summary-card/summary-card';
import { InstitutionAvatar } from '../../shared/ui/institutions/institution-avatar/institution-avatar';
import { PeriodSelector } from '../../shared/ui/period-selector/period-selector';
import { AccountPageService } from './account-page-service';
import { AccountSkeleton } from './components/account-skeleton/account-skeleton';

@Component({
  selector: 'app-account',
  imports: [
    PeriodSelector,
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
  private readonly pageService = inject(PageService);
  private readonly accountPageService = inject(AccountPageService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = this.accountPageService.loading;
  readonly error = this.accountPageService.error;
  readonly data = this.accountPageService.data;
  readonly selectedPeriod = this.accountPageService.selectedPeriod;

  readonly selectPeriod = (period: TimePeriod): void =>
    this.accountPageService.selectPeriod(period);

  readonly refetchAll = (): void => this.accountPageService.refetchAll();

  constructor() {
    this.route.params.subscribe((params) => {
      const parsed = Number.parseInt(params['institutionId'] ?? '0', 10);
      this.accountPageService.setInstitutionId(Number.isNaN(parsed) ? 0 : parsed);
    });

    effect(() => {
      const name = this.accountPageService.institutionName();
      this.pageService.setPageTitle(name || 'Accounts');
      this.pageService.setPageSubtitle('View your account summary and balances.');
    });
  }
}
