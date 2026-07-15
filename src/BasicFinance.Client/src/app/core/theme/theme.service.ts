import { BreakpointObserver } from '@angular/cdk/layout';
import { computed, DOCUMENT, inject, Injectable, linkedSignal, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { distinctUntilChanged, map } from 'rxjs';

type ColorTheme = 'light' | 'dark';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private readonly themeStorageKey = 'BASICFINANCE_THEME';
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly breakpointColorSchemePreference = '(prefers-color-scheme: dark)';

  private readonly document = inject(DOCUMENT);

  private readonly userSystemTheme = toSignal(
    this.breakpointObserver.observe(this.breakpointColorSchemePreference).pipe(
      map((x) => (x.matches ? 'dark' : 'light')),
      distinctUntilChanged(),
    ),
    { initialValue: 'light' as ColorTheme },
  );

  private readonly localStorageTheme = signal(this.getLocalStorageTheme());

  private readonly currentTheme = linkedSignal<
    { systemTheme: ColorTheme; localTheme: ColorTheme | null },
    ColorTheme
  >({
    source: () => ({ systemTheme: this.userSystemTheme(), localTheme: this.localStorageTheme() }),
    computation: (themes) =>
      themes.localTheme !== null ? themes.localTheme : themes.systemTheme,
  });

  readonly appTheme = computed(() => this.currentTheme());

  setAppTheme(colorTheme: ColorTheme): void {
    localStorage.setItem(this.themeStorageKey, colorTheme);
    this.localStorageTheme.set(colorTheme);
  }

  getAppColor(color: string): string {
    return window.getComputedStyle(this.document.documentElement).getPropertyValue(color).trim();
  }

  private getLocalStorageTheme(): ColorTheme | null {
    const localStorageTheme = localStorage.getItem(this.themeStorageKey);
    return localStorageTheme === 'light' || localStorageTheme === 'dark' ? localStorageTheme : null;
  }
}
