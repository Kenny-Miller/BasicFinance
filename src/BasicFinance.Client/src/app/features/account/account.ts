import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PageService } from '../../core/page/page.service';
import { ThemeService } from '../../core/theme/theme.service';

@Component({
  selector: 'app-account',
  imports: [],
  templateUrl: './account.html',
  styleUrl: './account.css',
})
export class Account implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly pageService = inject(PageService);
  private readonly themeService = inject(ThemeService);

  private readonly $routeParams = signal(this.route.snapshot.params);

  private readonly accountId = signal(0);

  readonly appTheme = this.themeService.appTheme;

  constructor() {}

  ngOnInit(): void {
    this.route.params.subscribe((params) => {
      this.accountId.set(parseInt(params['id'] ?? '0'));
      console.log('fetching data');

      this.pageService.setPageTitle(`My Account: ${this.accountId()}`);
      this.pageService.setPageSubtitle('View your account summary and spending details.');
    });
  }
}
