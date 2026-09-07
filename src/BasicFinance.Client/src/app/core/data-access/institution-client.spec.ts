import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
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

  it("should fetch the user's institutions when the client is created", async () => {
    // The httpResource effect that starts the request is scheduled when the client is
    // constructed; flush effects so the request is issued before it can be expected.
    TestBed.flushEffects();

    const request = controller.expectOne('api/my/institutions');

    expect(request.request.method).toBe('GET');

    request.flush([]);
    // The resource applies the response in an async continuation; let it settle first.
    await Promise.resolve();
    expect(client.myInstitutions.value()).toEqual([]);
  });

  it("should refetch the user's institutions when refetchMyInstitutions is called", async () => {
    TestBed.flushEffects();

    const firstRequest = controller.expectOne('api/my/institutions');
    firstRequest.flush([]);
    // reload() is a no-op until the resource has settled to a resolved state, which
    // happens in an async continuation after the response is flushed.
    await Promise.resolve();

    client.refetchMyInstitutions();
    TestBed.flushEffects();

    const secondRequest = controller.expectOne('api/my/institutions');
    secondRequest.flush([]);
    await Promise.resolve();
    expect(client.myInstitutions.hasValue()).toBeTruthy();
  });
});
