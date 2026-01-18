import { Component, CUSTOM_ELEMENTS_SCHEMA, inject, signal } from '@angular/core';
import '@googleworkspace/drive-picker-element';
import {
  OAuthErrorEvent,
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

  isGoogleFilePickerOpen = signal<boolean>(false);

  linkedGoogleSpreadsheets = signal([
    { id: '1', name: 'aaaaa' },
    { id: '2', name: 'asdfasd' },
    { id: '3', name: 'asdfsd' },
    { id: '4', name: 'sdaaa' },
  ]);

  public openGoogleFilePicker(): void {
    this.isGoogleFilePickerOpen.set(true);
  }

  public handleOAuthError(event: OAuthErrorEvent): void {
    this.isGoogleFilePickerOpen.set(false);
  }

  public handlePickerPicked(event: PickerPickedEvent): void {
    if (event.detail['docs'] === undefined) {
      this.isGoogleFilePickerOpen.set(false);
      return;
    }

    const spreadsheetId = event.detail['docs'][0]['id'];
    this.settingsClient.addSpreadSheet(spreadsheetId).subscribe({
      next: (res) => console.log(res),
      error: (e) => console.log(e),
      complete: () => this.isGoogleFilePickerOpen.set(false),
    });
  }

  public handlePickerCanceled(event: PickerCanceledEvent): void {
    this.isGoogleFilePickerOpen.set(false);
  }
}
