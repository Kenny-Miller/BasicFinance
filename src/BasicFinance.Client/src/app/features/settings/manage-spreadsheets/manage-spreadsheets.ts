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
  imports: [NgIcon, HlmButtonImports, HlmItemImports, HlmCardImports],
  templateUrl: './manage-spreadsheets.html',
  styleUrl: './manage-spreadsheets.css',
})
export class ManageSpreadsheets {
  private readonly settingsClient = inject(SettingsClient);
  readonly environmnetConfig = inject(ENVIRONMENT_CONFIG);

  readonly spreadsheetResource = this.settingsClient.spreadsheetResource;

  readonly googleOAuthToken = signal<string | null>(null);
  readonly isGoogleFilePickerOpen = signal<boolean>(false);

  public openGoogleFilePicker(): void {
    this.isGoogleFilePickerOpen.set(true);
  }

  public handleOAuthResponse(event: OAuthResponseEvent): void {
    this.googleOAuthToken.set(event.detail.access_token);
  }

  public handleOAuthError(_event: OAuthErrorEvent): void {
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
      next: () => this.spreadsheetResource.reload(),
      error: (_e) => {
        this.isGoogleFilePickerOpen.set(false);
      },
      complete: () => {
        this.isGoogleFilePickerOpen.set(false);
        this.googleOAuthToken.set(null);
      },
    });
  }

  public handlePickerCanceled(_event: PickerCanceledEvent): void {
    this.isGoogleFilePickerOpen.set(false);
  }

  public deleteGoogleSpreadsheet(spreadsheetId: string) {
    this.settingsClient.deleteSpreadSheet(spreadsheetId).subscribe({
      next: () => this.spreadsheetResource.reload(),
    });
  }
}
