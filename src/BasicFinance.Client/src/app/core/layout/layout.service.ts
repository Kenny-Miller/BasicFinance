import { inject, Injectable } from '@angular/core';
import { map, distinctUntilChanged } from 'rxjs';
import { BreakpointObserver } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';

@Injectable({
  providedIn: 'root',
})
export class LayoutService {
  private readonly breakpointObserver = inject(BreakpointObserver);
  private readonly breakpointMedium = '(min-width: 40rem)';

  isMobile = toSignal(
    this.breakpointObserver.observe(this.breakpointMedium).pipe(
      map((x) => !x.matches || false),
      distinctUntilChanged(),
    ),
    { initialValue: false }
  )
}
