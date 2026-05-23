import { TestBed } from '@angular/core/testing';

import { HomeClient } from './home-client';

describe('HomeClient', () => {
  let service: HomeClient;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(HomeClient);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
