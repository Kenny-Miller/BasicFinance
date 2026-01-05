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
import { BrnNavigationMenuImports } from '@spartan-ng/brain/navigation-menu';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmIcon } from '@spartan-ng/helm/icon';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { HlmNavigationMenuImports } from '@spartan-ng/helm/navigation-menu';
import { NavMenuItem } from '../../core/menu/nav-menu-item';

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
    HlmIcon,
    NgIcon,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    BrnNavigationMenuImports,
    HlmNavigationMenuImports,
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
