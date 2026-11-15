import { Component, inject, input } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideDot } from '@ng-icons/lucide';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmMarkerImports } from '@spartan-ng/helm/marker';
import { HlmSidebarImports, HlmSidebarService } from '@spartan-ng/helm/sidebar';

@Component({
  imports: [NgIcon, HlmBadgeImports, HlmMarkerImports, HlmSidebarImports],
  providers: [provideIcons({ lucideDot })],
  selector: 'app-page-header',
  styleUrl: './app-page-header.css',
  templateUrl: './app-page-header.html',
})
export class AppPageHeader {
  private readonly _sidebarService = inject(HlmSidebarService);

  readonly pageTitle = input.required<string>();
  readonly isMobile = this._sidebarService.isMobile;
}
