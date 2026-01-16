import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class SettingsClient {
  client = inject(HttpClient);

  getSpreadSheets(): Observable<any> {
    return this.client.get("api/settings/spreadsheets").pipe(
      catch((e) => )
    )
  }
}
