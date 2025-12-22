import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HlmSidebarImports, HlmSidebarService } from '@spartan-ng/helm/sidebar';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideLayoutDashboard, lucideWallet, lucideRepeat, lucideReceiptText, lucideLandmark, lucideChartLine, lucideSettings, lucideChevronLeft, lucideChevronRight } from '@ng-icons/lucide';
import { HlmIcon } from '@spartan-ng/helm/icon';
import { HlmSeparatorImports } from '@spartan-ng/helm/separator';


@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule, HlmSidebarImports, NgIcon, HlmIcon, HlmSeparatorImports],
  providers: [provideIcons({ lucideLayoutDashboard, lucideWallet, lucideRepeat, lucideReceiptText, lucideLandmark, lucideChartLine, lucideSettings, lucideChevronLeft, lucideChevronRight })],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {

  protected readonly title = signal('BasicFinance.Client');

  private readonly sidebarService = inject(HlmSidebarService)
  readonly desktopSidebarIsOpen = computed(() => this.sidebarService.open());
  readonly isMobile = computed(() => this.sidebarService.isMobile());

  readonly navigationItems: any[] = [
    { label: 'Dashboard', icon: 'lucideLayoutDashboard' },
    { label: 'Accounts', icon: 'lucideLandmark' },
    { label: 'Recurring', icon: 'lucideRepeat' },
    { label: 'Transactions', icon: 'lucideReceiptText' },
    { label: 'Spending', icon: 'lucideWallet' }
  ];

  readonly settingItem: any = { label: 'Settings', icon: 'lucideSettings' }

  public toggleOpen(): void {
    this.sidebarService.setOpen(!this.desktopSidebarIsOpen())
  }
}


