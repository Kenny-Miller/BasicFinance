import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ListResult } from './../../shared/api/list-result';

import { RouterLink } from '@angular/router';
import { provideIcons } from '@ng-icons/core';
import {
  lucideChartNoAxesCombined,
  lucideCreditCard,
  lucideDollarSign,
  lucideLandmark,
} from '@ng-icons/lucide';
import { HlmAccordionImports } from '@spartan-ng/helm/accordion';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { OAuthService } from 'angular-oauth2-oidc';
import { AuthUserProfile, AuthUserProfileResponse } from '../../core/auth/auth-userprofile';
import { PageService } from '../../core/page/page.service';
import { ThemeService } from '../../core/theme/theme.service';
import { AccountTypeGroup } from '../../shared/api/accounts/accountByType';
import { SummaryCardSkeleton } from '../../shared/ui/cards/summary-card-skeleton/summary-card-skeleton';
import { SummaryCard } from '../../shared/ui/cards/summary-card/summary-card';
import { TransactionsListSkeleton } from '../../shared/ui/transactions/transactions-list-skeleton/transactions-list-skeleton';
import { TransactionsList } from '../../shared/ui/transactions/transactions-list/transactions-list';
import { AccountGroupAccordion } from './components/account-group-accordion/account-group-accordion';
import { SpendActivityChartSkeleton } from './components/spend-activity-chart-skeleton/spend-activity-chart-skeleton';
import { SpendActivityChart } from './components/spend-activity-chart/spend-activity-chart';
import { HomeClient } from './data/home-client';

@Component({
  selector: 'app-home',
  providers: [
    provideIcons({ lucideChartNoAxesCombined, lucideCreditCard, lucideLandmark, lucideDollarSign }),
  ],
  imports: [
    RouterLink,
    CommonModule,
    HlmCardImports,
    HlmAccordionImports,
    TransactionsList,
    TransactionsListSkeleton,
    HlmButtonImports,
    AccountGroupAccordion,
    SpendActivityChart,
    SpendActivityChartSkeleton,
    SummaryCard,
    SummaryCardSkeleton,
  ],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  private readonly oauthService = inject(OAuthService);
  private readonly homeClient = inject(HomeClient);
  private readonly pageService = inject(PageService);
  private readonly themeService = inject(ThemeService);

  readonly accountsByTypeResource = this.homeClient.accountsByTypeResource;
  readonly transactionsResource = this.homeClient.transactionsResource;
  readonly spendingOverTimeResource = this.homeClient.spendingOverTimeResource;
  readonly netWorthSummaryResource = this.homeClient.netWorthSummaryResource;

  readonly checkingAccountsDisplay = computed(() =>
    this.processAccounts(this.accountsByTypeResource.value(), 'CHK'),
  );
  readonly savingsAccountsDisplay = computed(() =>
    this.processAccounts(this.accountsByTypeResource.value(), 'SAV'),
  );
  readonly creditCardAccountsDisplay = computed(() =>
    this.processAccounts(this.accountsByTypeResource.value(), 'CRD'),
  );
  readonly investmentAccountsDisplay = computed(() =>
    this.processAccounts(this.accountsByTypeResource.value(), 'INV'),
  );

  readonly appTheme = this.themeService.appTheme;
  readonly user = signal<AuthUserProfile | null>(null);
  readonly currentDate = new Date();
  readonly welcomeText = computed(() =>
    this.currentDate.getHours() < 12
      ? `Good Morning ${this.user()?.given_name}`
      : `Good Afternoon ${this.user()?.given_name}`,
  );

  async ngOnInit() {
    this.pageService.setPageTitle('Home');
    this.pageService.setPageSubtitle('View your financial overview');
    const response = (await this.oauthService.loadUserProfile()) as AuthUserProfileResponse;
    this.user.set(response.info);
  }

  private processAccounts(
    accountResourceResult: ListResult<AccountTypeGroup> | undefined,
    accountCode: string,
  ) {
    const accountsOfType = accountResourceResult?.items.find(
      (group) => group.accountTypeCode === accountCode,
    );
    return accountsOfType ?? { accounts: [], totalBalance: 0 };
  }
}
