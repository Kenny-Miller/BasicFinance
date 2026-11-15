import { DOCUMENT } from '@angular/common';
import { DestroyRef, Injectable, inject, signal } from '@angular/core';

import { isSameDay, startOfTomorrow } from 'date-fns';

/**
 * The current local date as a signal. Rolls over at local midnight, and re-checks when the
 * tab becomes visible again because timers are throttled in background tabs.
 */
@Injectable({
  providedIn: 'root',
})
export class TodayService {
  private readonly document = inject(DOCUMENT);
  private readonly _today = signal(new Date());
  private timer: ReturnType<typeof setTimeout> | undefined;

  readonly today = this._today.asReadonly();

  constructor() {
    this.scheduleRollover();

    const onVisibilityChange = (): void => {
      if (this.document.visibilityState === 'visible') {
        this.refresh();
      }
    };
    this.document.addEventListener('visibilitychange', onVisibilityChange);

    inject(DestroyRef).onDestroy(() => {
      clearTimeout(this.timer);
      this.document.removeEventListener('visibilitychange', onVisibilityChange);
    });
  }

  private refresh(): void {
    const now = new Date();
    if (!isSameDay(now, this._today())) {
      this._today.set(now);
    }
    this.scheduleRollover();
  }

  private scheduleRollover(): void {
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.refresh(), startOfTomorrow().getTime() - Date.now());
  }
}
