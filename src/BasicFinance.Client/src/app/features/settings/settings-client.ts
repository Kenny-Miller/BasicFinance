import { HttpClient, HttpHeaders, httpResource } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ListResult } from '../../shared/api/list-result';
import { Spreadsheet } from '../../shared/api/spreadsheets/spreadsheet';

@Injectable({
  providedIn: 'root',
})
export class SettingsClient {
  client = inject(HttpClient);

  spreadsheetResource = httpResource<ListResult<Spreadsheet>>(() => 'api/spreadsheets');

  addSpreadSheet(googleSpreadsheetId: string, googleOAuthToken: string): Observable<void> {
    const request = {
      googleSpreadsheetId: googleSpreadsheetId,
    };

    const httpHeader = new HttpHeaders().append('x-google-auth-token', googleOAuthToken);
    return this.client.post<void>('api/spreadsheets', request, { headers: httpHeader });
  }

  deleteSpreadSheet(spreadsheetId: string): Observable<void> {
    return this.client.delete<void>(`api/spreadsheets/${spreadsheetId}`);
  }
}
