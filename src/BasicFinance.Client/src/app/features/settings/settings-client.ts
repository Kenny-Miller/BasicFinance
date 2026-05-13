import { HttpClient, HttpHeaders, httpResource } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Spreadsheet } from '../../core/spreadsheets/spreadsheet';
import { ListResult } from '../../shared/result/list-result';

@Injectable({
  providedIn: 'root',
})
export class SettingsClient {
  client = inject(HttpClient);

  spreadsheetResource = httpResource<ListResult<Spreadsheet>>(() => 'api/spreadsheets');

  addSpreadSheet(googleSpreadsheetId: string, googleOAuthToken: string): Observable<any> {
    const request = {
      googleSpreadsheetId: googleSpreadsheetId,
    };

    const httpHeader = new HttpHeaders().append('x-google-auth-token', googleOAuthToken);
    return this.client.post('api/spreadsheets', request, { headers: httpHeader });
  }

  deleteSpreadSheet(spreadsheetId: string): Observable<any> {
    return this.client.delete(`api/spreadsheets/${spreadsheetId}`);
  }
}
