import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideChartLine,
  lucideChartPie,
  lucideChevronLeft,
  lucideChevronRight,
  lucideHouse,
  lucideMoon,
  lucidePlus,
  lucideReceiptText,
  lucideSettings,
  lucideSun,
} from '@ng-icons/lucide';
import { HlmSeparator, HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { HlmSidebarImports, HlmSidebarService } from '@spartan-ng/helm/sidebar';

import { PageService } from './core/page/page.service';
import { ThemeService } from './core/theme/theme.service';
import { NavMenuItem } from './shared/models/nav-menu-item';
import { InstitutionAvatar } from './shared/ui/institutions/institution-avatar/institution-avatar';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    CommonModule,
    HlmSidebarImports,
    NgIcon,
    RouterLink,
    RouterLinkActive,
    InstitutionAvatar,
    HlmSeparatorImports,
    HlmSeparator,
  ],
  providers: [
    provideIcons({
      lucideHouse,
      lucideChartPie,
      lucideReceiptText,
      lucideChartLine,
      lucideSettings,
      lucideChevronLeft,
      lucideChevronRight,
      lucideMoon,
      lucideSun,
      lucidePlus,
    }),
  ],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly _pageService = inject(PageService);
  private readonly _sidebarService = inject(HlmSidebarService);
  private readonly _themeService = inject(ThemeService);

  readonly pageTitle = this._pageService.pageTitle;
  readonly theme = this._themeService.appTheme;
  readonly isDesktopSidebarOpen = this._sidebarService.open;
  readonly isMobile = this._sidebarService.isMobile;

  readonly navigationItems: NavMenuItem[] = [
    { label: 'Home', icon: 'lucideHouse', routerLink: '' },
    { label: 'Transactions', icon: 'lucideReceiptText', routerLink: 'Transactions' },
    { label: 'Spending', icon: 'lucideChartPie', routerLink: 'Spending' },
  ];

  readonly accountNavigationItems = computed(
    () =>
      this._pageService.data()?.map(institution => ({
        label: institution.name,
        icon: 'lucideLandmark',
        routerLink: `Accounts/${institution.institutionId}`,
        activeAccounts: institution.activeAccounts,
        totalBalance: institution.totalBalance,
      })) ?? [],
  );

  readonly institutionsLoading = this._pageService.loading;

  public toggleSidebarOpen(): void {
    this._sidebarService.setOpen(!this.isDesktopSidebarOpen());
  }

  public toggleTheme(): void {
    this._themeService.setAppTheme(this.theme() === 'dark' ? 'light' : 'dark');
  }
}
