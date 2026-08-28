import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';

import { HlmButtonImports } from '@spartan-ng/helm/button';
import { OAuthService } from 'angular-oauth2-oidc';
import { AuthUserProfile, AuthUserProfileResponse } from '../../core/auth/auth-userprofile';
import { PageService } from '../../core/page/page.service';
import { ThemeService } from '../../core/theme/theme.service';
import { SummaryCard } from '../../shared/ui/cards/summary-card/summary-card';
import { AccountNetWorthBreakdown } from './components/account-net-worth-breakdown/account-net-worth-breakdown';
import { HomeSkeleton } from './components/home-skeleton/home-skeleton';
import { RecentTransactions } from './components/recent-transactions/recent-transactions';
import { SpendActivityChart } from './components/spend-activity-chart/spend-activity-chart';
import { HomeService } from './home-service';

@Component({
  selector: 'app-home',
  imports: [
    DatePipe,
    HlmButtonImports,
    SummaryCard,
    AccountNetWorthBreakdown,
    SpendActivityChart,
    HomeSkeleton,
    RecentTransactions,
  ],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  private readonly oauthService = inject(OAuthService);
  private readonly homeService = inject(HomeService);
  private readonly pageService = inject(PageService);
  private readonly themeService = inject(ThemeService);

  readonly loading = this.homeService.loading;
  readonly error = this.homeService.error;

  readonly currentNetWorth = this.homeService.currentNetWorth;
  readonly previousNetWorth = this.homeService.previousNetWorth;
  readonly currentChecking = this.homeService.currentChecking;
  readonly previousChecking = this.homeService.previousChecking;
  readonly currentSavings = this.homeService.currentSavings;
  readonly previousSavings = this.homeService.previousSavings;
  readonly currentInvestments = this.homeService.currentInvestments;
  readonly previousInvestments = this.homeService.previousInvestments;

  readonly currentPeriodBreakdown = this.homeService.currentPeriodBreakdown;
  readonly spendingOverTimeData = this.homeService.spendingOverTimeData;
  readonly recentTransactions = this.homeService.recentTransactions;

  readonly refetchAll = (): void => this.homeService.refetchAll();

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
