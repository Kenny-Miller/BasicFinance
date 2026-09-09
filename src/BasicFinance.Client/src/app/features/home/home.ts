import { Component, inject, OnInit, signal } from '@angular/core';

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
  readonly data = this.homeService.data;
  readonly accountTypes = this.pageService.accountTypes;

  readonly refetchAll = (): void => this.homeService.refetchAll();

  readonly appTheme = this.themeService.appTheme;
  readonly user = signal<AuthUserProfile | null>(null);

  async ngOnInit() {
    this.pageService.setPageTitle('Home');
    this.pageService.setPageSubtitle('View your financial overview');
    const response = (await this.oauthService.loadUserProfile()) as AuthUserProfileResponse;
    this.user.set(response.info);
  }
}
