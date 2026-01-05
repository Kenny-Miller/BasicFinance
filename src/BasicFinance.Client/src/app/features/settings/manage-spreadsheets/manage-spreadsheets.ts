import { JsonPipe } from '@angular/common';
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
  imports: [HlmIcon, HlmButtonImports, HlmItemImports, NgIcon, HlmCardImports, JsonPipe],
  templateUrl: './manage-spreadsheets.html',
  styleUrl: './manage-spreadsheets.css',
})
export class ManageSpreadsheets {
  environmnetConfig = inject(ENVIRONMENT_CONFIG);
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
    console.log(event);
    this.isGoogleFilePickerOpen.set(false);
  }

  public handlePickerCanceled(event: PickerCanceledEvent): void {
    console.log(event);
    this.isGoogleFilePickerOpen.set(false);
  }
}
