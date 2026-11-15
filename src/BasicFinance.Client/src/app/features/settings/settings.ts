import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideChevronRight,
  lucideFileSpreadsheet,
  lucidePlus,
  lucideShield,
  lucideTrash,
} from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmItemImports } from '@spartan-ng/helm/item';

import { NavMenuItem } from '../../shared/models/nav-menu-item';
import { AppPageHeader } from '../../shared/ui/app-page-header/app-page-header';

@Component({
  selector: 'app-settings',
  providers: [
    provideIcons({
      lucideFileSpreadsheet,
      lucideShield,
      lucideChevronRight,
      lucideTrash,
      lucidePlus,
    }),
  ],
  imports: [
    HlmCardImports,
    HlmButtonImports,
    HlmItemImports,
    NgIcon,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    AppPageHeader,
  ],
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings {
  readonly navigationItems: NavMenuItem[] = [
    { label: 'Manage Spreadsheets', icon: 'lucideFileSpreadsheet', routerLink: './spreadsheets' },
    { label: 'Account', icon: 'lucideShield', routerLink: './account' },
  ];
}
