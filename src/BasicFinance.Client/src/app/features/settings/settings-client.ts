import { HttpClient } from '@angular/common/http';
import { Injectable, inject, resource } from '@angular/core';

import {
  deleteApiSpreadsheetsSpreadsheetId,
  getApiSpreadsheets,
  postApiSpreadsheets,
} from '../../core/api/generated/sdk.gen';
import { normalizeListResult } from '../../core/api/list-result';

@Injectable({
  providedIn: 'root',
})
export class SettingsClient {
  private readonly httpClient = inject(HttpClient);

  spreadsheetResource = resource({
    loader: async () => {
      const response = await getApiSpreadsheets({
        httpClient: this.httpClient,
        throwOnError: true,
      });
      return normalizeListResult(response.data);
    },
  });

  async addSpreadSheet(googleSpreadsheetId: string, googleOAuthToken: string): Promise<void> {
    await postApiSpreadsheets({
      body: { googleSpreadsheetId },
      headers: { 'x-google-auth-token': googleOAuthToken },
      httpClient: this.httpClient,
      throwOnError: true,
    });
  }

  async deleteSpreadSheet(spreadsheetId: string): Promise<void> {
    await deleteApiSpreadsheetsSpreadsheetId({
      httpClient: this.httpClient,
      path: { spreadsheetId },
      throwOnError: true,
    });
  }
}
