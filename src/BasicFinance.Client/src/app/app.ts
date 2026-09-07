import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideChartLine,
  lucideChevronLeft,
  lucideChevronRight,
  lucideLandmark,
  lucideLayoutDashboard,
  lucideReceiptText,
  lucideRepeat,
  lucideSettings,
  lucideWallet,
} from '@ng-icons/lucide';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { HlmSidebarImports, HlmSidebarService } from '@spartan-ng/helm/sidebar';
import { PageService } from './core/page/page.service';
import { NavMenuItem } from './shared/models/nav-menu-item';
import { InstitutionAvatar } from './shared/ui/institutions/institution-avatar/institution-avatar';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    CommonModule,
    HlmSidebarImports,
    NgIcon,
    HlmSeparatorImports,
    RouterLink,
    RouterLinkActive,
    InstitutionAvatar,
  ],
  providers: [
    provideIcons({
      lucideLayoutDashboard,
      lucideWallet,
      lucideRepeat,
      lucideReceiptText,
      lucideLandmark,
      lucideChartLine,
      lucideSettings,
      lucideChevronLeft,
      lucideChevronRight,
    }),
  ],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly _pageService = inject(PageService);
  private readonly _sidebarService = inject(HlmSidebarService);

  readonly pageTitle = this._pageService.pageTitle;
  readonly pageSubtitle = this._pageService.pageSubtitle;
  readonly isDesktopSidebarOpen = this._sidebarService.open;
  readonly isMobile = this._sidebarService.isMobile;

  readonly navigationItems: NavMenuItem[] = [
    { label: 'Home', icon: 'lucideLayoutDashboard', routerLink: '' },
    { label: 'Transactions', icon: 'lucideReceiptText', routerLink: 'Transactions' },
    { label: 'Spending', icon: 'lucideWallet', routerLink: 'Spending' },
  ];

  readonly accountNavigationItems = computed<NavMenuItem[]>(
    () =>
      this._pageService.data()?.map((institution) => ({
        label: institution.name,
        icon: 'lucideLandmark',
        routerLink: `Accounts/${institution.id}`,
      })) ?? [],
  );

  readonly institutionsLoading = this._pageService.loading;

  public toggleSidebarOpen(): void {
    this._sidebarService.setOpen(!this.isDesktopSidebarOpen());
  }
}
