import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ENVIRONMENT_CONFIG } from '../../../environment-config';
import { SettingsClient } from '../settings-client';
import { ManageSpreadsheets } from './manage-spreadsheets';

describe('ManageSpreadsheets', () => {
  let component: ManageSpreadsheets;
  let fixture: ComponentFixture<ManageSpreadsheets>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManageSpreadsheets],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        {
          provide: ENVIRONMENT_CONFIG,
          useValue: {
            basicFinanceApi: 'http://localhost:5001',
            openIdAuthority: 'https://localhost:8080/realms/basic-hub',
            openIdClientId: 'basic-finance-client',
          },
        },
        {
          provide: SettingsClient,
          useValue: {
            spreadsheetResource: {
              value: () => ({ items: [], totalCount: 0, page: 1 }),
              hasValue: () => false,
              error: () => null,
              isLoading: () => false,
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ManageSpreadsheets);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
