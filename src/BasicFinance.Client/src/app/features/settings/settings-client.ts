import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class SettingsClient {
  client = inject(HttpClient);
  oauthService = inject(OAuthService);

  getSpreadSheets(): Observable<any> {
    return this.client.get('api/spreadsheets').pipe();
  }

  addSpreadSheet(spreadsheetId: string, googleOAuthToken: string): Observable<any> {
    const request = {
      spreadsheetId: spreadsheetId,
    };

    const token = this.oauthService.getAccessToken();
    const httpHeader = new HttpHeaders()
      .append('Authorization', `Bearer ${token}`)
      .append('x-google-auth-token', googleOAuthToken);

    return this.client.post('api/spreadsheets', request, { headers: httpHeader }).pipe();
  }
}
