import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
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
import { HlmIcon } from '@spartan-ng/helm/icon';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';
import { HlmSidebarImports, HlmSidebarService } from '@spartan-ng/helm/sidebar';
import { NavMenuItem } from './core/menu/nav-menu-item';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    CommonModule,
    HlmSidebarImports,
    NgIcon,
    HlmIcon,
    HlmSeparatorImports,
    RouterLink,
    RouterLinkActive,
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
  protected readonly title = signal('BasicFinance.Client');

  private readonly sidebarService = inject(HlmSidebarService);
  readonly isDesktopSidebarOpen = computed(() => this.sidebarService.open());
  readonly isMobile = computed(() => this.sidebarService.isMobile());

  readonly navigationItems: NavMenuItem[] = [
    { label: 'Home', icon: 'lucideLayoutDashboard', routerLink: '' },
    { label: 'Accounts', icon: 'lucideLandmark', routerLink: 'accounts' },
    { label: 'Recurring', icon: 'lucideRepeat', routerLink: 'recurring' },
    { label: 'Transactions', icon: 'lucideReceiptText', routerLink: 'transactions' },
    { label: 'Spending', icon: 'lucideWallet', routerLink: 'spending' },
  ];

  public toggleSidebarOpen(): void {
    this.sidebarService.setOpen(!this.isDesktopSidebarOpen());
  }
}
