import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HlmSidebarService } from '@spartan-ng/helm/sidebar';
import { vi } from 'vite-plus/test';

import { AppPageHeader } from './app-page-header';

describe('AppPageHeader', () => {
  let fixture: ComponentFixture<AppPageHeader>;
  const isMobile = signal(false);
  const toggleSidebar = vi.fn();

  const element = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const trigger = (): HTMLButtonElement | null =>
    element().querySelector('button[data-sidebar="trigger"], button[hlmSidebarTrigger]');

  beforeEach(async () => {
    isMobile.set(false);
    toggleSidebar.mockReset();

    await TestBed.configureTestingModule({
      imports: [AppPageHeader],
      providers: [
        {
          provide: HlmSidebarService,
          useValue: { isMobile, toggleSidebar, open: signal(false) },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppPageHeader);
    fixture.componentRef.setInput('pageTitle', 'Transactions');
    await fixture.whenStable();
  });

  it('should render the page title', () => {
    expect(element().textContent).toContain('Transactions');
  });

  it('should hide the sidebar trigger on desktop', () => {
    expect(trigger()).toBeNull();
  });

  it('should toggle the sidebar from the trigger on mobile', async () => {
    isMobile.set(true);
    await fixture.whenStable();

    trigger()?.click();

    expect(toggleSidebar).toHaveBeenCalledTimes(1);
  });
});
