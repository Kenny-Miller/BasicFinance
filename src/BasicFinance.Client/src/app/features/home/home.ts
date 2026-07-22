import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';

import { provideIcons } from '@ng-icons/core';
import {
  lucideChartNoAxesCombined,
  lucideCreditCard,
  lucideDollarSign,
  lucideLandmark,
} from '@ng-icons/lucide';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { OAuthService } from 'angular-oauth2-oidc';
import { AuthUserProfile, AuthUserProfileResponse } from '../../core/auth/auth-userprofile';
import { PageService } from '../../core/page/page.service';
import { ThemeService } from '../../core/theme/theme.service';
import { SummaryCardSkeleton } from '../../shared/ui/cards/summary-card-skeleton/summary-card-skeleton';
import { SummaryCard } from '../../shared/ui/cards/summary-card/summary-card';
import { AccountNetWorthBreakdownSkeleton } from './components/account-net-worth-breakdown-skeleton/account-net-worth-breakdown-skeleton';
import { AccountNetWorthBreakdown } from './components/account-net-worth-breakdown/account-net-worth-breakdown';
import { RecentTransactionsSkeleton } from './components/recent-transactions-skeleton/recent-transactions-skeleton';
import { RecentTransactions } from './components/recent-transactions/recent-transactions';
import { SpendActivityChartSkeleton } from './components/spend-activity-chart-skeleton/spend-activity-chart-skeleton';
import { SpendActivityChart } from './components/spend-activity-chart/spend-activity-chart';
import { HomeClient } from './data/home-client';

@Component({
  selector: 'app-home',
  providers: [
    provideIcons({ lucideChartNoAxesCombined, lucideCreditCard, lucideLandmark, lucideDollarSign }),
  ],
  imports: [
    CommonModule,
    HlmCardImports,
    AccountNetWorthBreakdown,
    SpendActivityChart,
    SpendActivityChartSkeleton,
    SummaryCard,
    SummaryCardSkeleton,
    AccountNetWorthBreakdownSkeleton,
    RecentTransactions,
    RecentTransactionsSkeleton,
  ],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  private readonly oauthService = inject(OAuthService);
  private readonly homeClient = inject(HomeClient);
  private readonly pageService = inject(PageService);
  private readonly themeService = inject(ThemeService);

  readonly balanceSummaryResource = this.homeClient.balanceSummaryResource;
  readonly transactionsResource = this.homeClient.transactionsResource;
  readonly recentTransactions = computed(() => this.transactionsResource.value()?.items ?? []);
  readonly spendingOverTimeResource = this.homeClient.spendingOverTimeResource;

  readonly currentNetWorth = computed(
    () => this.balanceSummaryResource.value()?.currentPeriodBreakdown.balance ?? 0,
  );
  readonly previousNetWorth = computed(
    () => this.balanceSummaryResource.value()?.previousPeriodBreakdown.balance ?? 0,
  );

  readonly currentChecking = computed(
    () =>
      this.balanceSummaryResource.value()?.currentPeriodBreakdown.accountTypeBreakdowns['CHK']
        ?.balance ?? 0,
  );
  readonly previousChecking = computed(
    () =>
      this.balanceSummaryResource.value()?.previousPeriodBreakdown.accountTypeBreakdowns['CHK']
        ?.balance ?? 0,
  );

  readonly currentSavings = computed(
    () =>
      this.balanceSummaryResource.value()?.currentPeriodBreakdown.accountTypeBreakdowns['SAV']
        ?.balance ?? 0,
  );
  readonly previousSavings = computed(
    () =>
      this.balanceSummaryResource.value()?.previousPeriodBreakdown.accountTypeBreakdowns['SAV']
        ?.balance ?? 0,
  );

  readonly currentInvestments = computed(
    () =>
      this.balanceSummaryResource.value()?.currentPeriodBreakdown.accountTypeBreakdowns['INV']
        ?.balance ?? 0,
  );
  readonly previousInvestments = computed(
    () =>
      this.balanceSummaryResource.value()?.previousPeriodBreakdown.accountTypeBreakdowns['INV']
        ?.balance ?? 0,
  );

  readonly currentPeriodBreakdown = computed(
    () =>
      this.balanceSummaryResource.value()?.currentPeriodBreakdown ?? {
        balance: 0,
        accountTypeBreakdowns: {},
      },
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
}
