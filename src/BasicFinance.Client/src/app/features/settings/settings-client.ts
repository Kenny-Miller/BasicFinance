import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class SettingsClient {
  client = inject(HttpClient);

  getSpreadSheets(): Observable<any> {
    return this.client.get('api/settings/spreadsheets').pipe();
  }

  addSpreadSheet(spreadsheetId: string): Observable<any> {
    const request = {
      spreadsheetId: spreadsheetId,
    };

    return this.client.post('api/spreadsheets', request).pipe();
  }
}
