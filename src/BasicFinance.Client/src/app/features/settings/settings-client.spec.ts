import { TestBed } from '@angular/core/testing';

import { SettingsClient } from './settings-client';

describe('SettingsClient', () => {
  let service: SettingsClient;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SettingsClient);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
