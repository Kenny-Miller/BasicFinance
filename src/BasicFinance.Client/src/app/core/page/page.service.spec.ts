import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { Institution, InstitutionClient } from '../data-access/institution-client';
import { PageService } from './page.service';

describe('PageService', () => {
  let service: PageService;

  let institutionsValue = signal<Institution[] | null>(null);
  let institutionsHasValue = signal(false);
  let institutionsError = signal<null | Error>(null);
  let refetchInstitutions = vi.fn();

  beforeEach(() => {
    institutionsValue = signal(null);
    institutionsHasValue = signal(false);
    institutionsError = signal(null);
    refetchInstitutions = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        {
          provide: InstitutionClient,
          useValue: {
            myInstitutions: {
              hasValue: () => institutionsHasValue(),
              value: () => institutionsValue(),
              error: () => institutionsError(),
              reload: () => true,
            },
            refetchMyInstitutions: () => {
              refetchInstitutions();
            },
          },
        },
      ],
    });
    service = TestBed.inject(PageService);
  });

  it('should report loading with no data until the institutions have settled', () => {
    expect(service.loading()).toBe(true);
    expect(service.data()).toBeNull();
    expect(service.error()).toBeFalsy();
  });

  it('should expose the institutions value and stop loading once they have loaded', () => {
    const institutions: Institution[] = [{ id: 1, code: 'WF', name: 'Wells Fargo', logoUrl: null }];
    institutionsValue.set(institutions);
    institutionsHasValue.set(true);

    expect(service.loading()).toBe(false);
    expect(service.data()).toEqual(institutions);
  });

  it('should stop loading and surface the error when the institutions fetch errors', () => {
    institutionsError.set(new Error('no institutions'));

    expect(service.loading()).toBe(false);
    expect(service.error()).toBeTruthy();
  });

  it('should keep the current institutions value while a refetch is requested', () => {
    const institutions: Institution[] = [{ id: 1, code: 'WF', name: 'Wells Fargo', logoUrl: null }];
    institutionsValue.set(institutions);
    institutionsHasValue.set(true);

    service.refetchAll();

    expect(service.data()).toEqual(institutions);
    expect(refetchInstitutions).toHaveBeenCalledTimes(1);
  });

  it('should update the page title and subtitle', () => {
    service.setPageTitle('Home');
    service.setPageSubtitle('Your dashboard');

    expect(service.pageTitle()).toBe('Home');
    expect(service.pageSubtitle()).toBe('Your dashboard');
  });
});
