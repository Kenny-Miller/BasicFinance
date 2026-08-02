import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PageService {
  private readonly _pageTitle = signal<string>('');
  private readonly _pageSubtitle = signal<string>('');

  readonly pageTitle = this._pageTitle.asReadonly();
  readonly pageSubtitle = this._pageSubtitle.asReadonly();

  setPageTitle(title: string) {
    this._pageTitle.set(title);
  }

  setPageSubtitle(subtitle: string) {
    this._pageSubtitle.set(subtitle);
  }
}
