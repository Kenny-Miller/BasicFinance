import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { settleSdkBridge } from '../testing/testbed-utils';
import { InstitutionClient } from './institution-client';

describe('InstitutionClient', () => {
  let client: InstitutionClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClientTesting()],
    });
    controller = TestBed.inject(HttpTestingController);
    client = TestBed.inject(InstitutionClient);
  });

  it("should fetch the user's institutions when the resource is created", async () => {
    // resource asserts an injection context, so the provider call is wrapped the way
    // PageService's field initializer runs it.
    const myInstitutions = TestBed.runInInjectionContext(() => client.myInstitutions());
    await settleSdkBridge();

    const request = controller.expectOne('/api/my/institutions');

    expect(request.request.method).toBe('GET');

    request.flush([]);
    await settleSdkBridge();
    expect(myInstitutions.value()).toEqual([]);
  });

  it("should issue a new request when the user's institutions resource is reloaded", async () => {
    const myInstitutions = TestBed.runInInjectionContext(() => client.myInstitutions());
    await settleSdkBridge();

    const firstRequest = controller.expectOne('/api/my/institutions');
    firstRequest.flush([]);
    await settleSdkBridge();

    myInstitutions.reload();
    await settleSdkBridge();

    const secondRequest = controller.expectOne('/api/my/institutions');
    secondRequest.flush([]);
    await settleSdkBridge();
    expect(myInstitutions.hasValue()).toBeTruthy();
  });
});
