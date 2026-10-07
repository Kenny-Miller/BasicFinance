import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { settleSdkBridge } from '../../core/testing/testbed-utils';
import { SettingsClient } from './settings-client';

describe('SettingsClient', () => {
  let service: SettingsClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClientTesting()],
    });
    controller = TestBed.inject(HttpTestingController);
    service = TestBed.inject(SettingsClient);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch the user spreadsheets when the resource is created', async () => {
    await settleSdkBridge();

    const request = controller.expectOne('/api/spreadsheets');

    expect(request.request.method).toBe('GET');

    const spreadsheets = [
      {
        id: 'sheet-1',
        googleSheetId: 'g-123',
        googleSheetName: 'Budget',
        createdDate: '2026-01-01T00:00:00.000Z',
      },
    ];
    request.flush({ items: spreadsheets, totalCount: 1 });
    await settleSdkBridge();

    expect(service.spreadsheetResource.value()).toEqual({
      items: spreadsheets,
      page: 1,
      pageSize: 0,
      pageCount: 0,
      totalCount: 1,
    });
  });

  it('should post a new spreadsheet with the Google auth token', async () => {
    // The request is dispatched before the SDK promise can settle under the testing
    // backend, so it must be flushed before the call can resolve.
    const postPromise = service.addSpreadSheet('sheet-123', 'oauth-token-abc');
    await settleSdkBridge();

    // The eagerly created spreadsheetResource may also have dispatched its initial
    // list request for the same URL, so match against the POST specifically.
    const request = controller.expectOne(
      req => req.method === 'POST' && req.url === '/api/spreadsheets'
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ googleSpreadsheetId: 'sheet-123' });
    expect(request.request.headers.get('x-google-auth-token')).toEqual('oauth-token-abc');

    request.flush(null, { status: 201, statusText: 'Created' });
    await postPromise;
  });

  it('should delete the spreadsheet for the given id', async () => {
    const deletePromise = service.deleteSpreadSheet('sheet-123');
    await settleSdkBridge();

    const request = controller.expectOne('/api/spreadsheets/sheet-123');

    expect(request.request.method).toBe('DELETE');

    request.flush(null, { status: 204, statusText: 'No Content' });
    await deletePromise;
  });
});
