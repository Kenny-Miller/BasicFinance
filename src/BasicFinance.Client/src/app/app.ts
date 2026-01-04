import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
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

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule, HlmSidebarImports, NgIcon, HlmIcon, HlmSeparatorImports],
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

  readonly navigationItems: any[] = [
    { label: 'Home', icon: 'lucideLayoutDashboard', href: '' },
    { label: 'Accounts', icon: 'lucideLandmark', href: 'accounts' },
    { label: 'Recurring', icon: 'lucideRepeat', href: 'recurring' },
    { label: 'Transactions', icon: 'lucideReceiptText', href: 'transactions' },
    { label: 'Spending', icon: 'lucideWallet', href: 'spending' },
  ];

  readonly settingItem: any = { label: 'Settings', icon: 'lucideSettings', href: 'settings' };

  public toggleOpen(): void {
    this.sidebarService.setOpen(!this.isDesktopSidebarOpen());
  }
}
