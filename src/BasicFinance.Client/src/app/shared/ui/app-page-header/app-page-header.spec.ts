import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppPageHeader } from './app-page-header';

describe('AppPageHeader', () => {
  let component: AppPageHeader;
  let fixture: ComponentFixture<AppPageHeader>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppPageHeader],
    }).compileComponents();

    fixture = TestBed.createComponent(AppPageHeader);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('pageTitle', 'Transactions');
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the page title', () => {
    const header = fixture.nativeElement as HTMLElement;
    expect(header.textContent).toContain('Transactions');
  });
});
