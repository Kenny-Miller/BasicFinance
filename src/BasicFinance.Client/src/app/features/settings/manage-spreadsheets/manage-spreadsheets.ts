import { Component, CUSTOM_ELEMENTS_SCHEMA, inject, signal } from '@angular/core';
import '@googleworkspace/drive-picker-element';
import {
  OAuthErrorEvent,
  OAuthResponseEvent,
  PickerCanceledEvent,
  PickerPickedEvent,
} from '@googleworkspace/drive-picker-element';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideChevronRight,
  lucideFileSpreadsheet,
  lucidePlus,
  lucideShield,
  lucideTrash,
} from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmIcon } from '@spartan-ng/helm/icon';
import { HlmItemImports } from '@spartan-ng/helm/item';
import { ENVIRONMENT_CONFIG } from '../../../environment-config';
import { SettingsClient } from '../settings-client';

@Component({
  selector: 'app-manage-spreadsheets',
  providers: [
    provideIcons({
      lucideFileSpreadsheet,
      lucideShield,
      lucideChevronRight,
      lucideTrash,
      lucidePlus,
    }),
  ],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  imports: [HlmIcon, HlmButtonImports, HlmItemImports, NgIcon, HlmCardImports],
  templateUrl: './manage-spreadsheets.html',
  styleUrl: './manage-spreadsheets.css',
})
export class ManageSpreadsheets {
  environmnetConfig = inject(ENVIRONMENT_CONFIG);
  settingsClient = inject(SettingsClient);

  googleOAuthToken = signal<string | null>(null);
  isGoogleFilePickerOpen = signal<boolean>(false);

  spreadsheets = this.settingsClient.spreadsheetResource;

  public openGoogleFilePicker(): void {
    this.isGoogleFilePickerOpen.set(true);
  }

  public handleOAuthResponse(event: OAuthResponseEvent): void {
    this.googleOAuthToken.set(event.detail.access_token);
  }

  public handleOAuthError(event: OAuthErrorEvent): void {
    this.googleOAuthToken.set(null);
    this.isGoogleFilePickerOpen.set(false);
  }

  public handlePickerPicked(event: PickerPickedEvent): void {
    const googleOAuthToken = this.googleOAuthToken();
    if (event.detail['docs'] === undefined || googleOAuthToken === null) {
      this.isGoogleFilePickerOpen.set(false);
      return;
    }

    const googleSpreadsheetId = event.detail['docs'][0]['id'];
    this.settingsClient.addSpreadSheet(googleSpreadsheetId, googleOAuthToken).subscribe({
      next: () => this.spreadsheets.reload(),
      error: (e) => {
        this.isGoogleFilePickerOpen.set(false);
      },
      complete: () => {
        this.isGoogleFilePickerOpen.set(false);
        this.googleOAuthToken.set(null);
      },
    });
  }

  public handlePickerCanceled(event: PickerCanceledEvent): void {
    this.isGoogleFilePickerOpen.set(false);
  }

  public deleteGoogleSpreadsheet(spreadsheetId: string) {
    this.settingsClient.deleteSpreadSheet(spreadsheetId).subscribe({
      next: () => this.spreadsheets.reload(),
    });
  }
}
