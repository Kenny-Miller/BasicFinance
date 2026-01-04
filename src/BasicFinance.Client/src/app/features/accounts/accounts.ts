import { Component, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import '@googleworkspace/drive-picker-element';

@Component({
  selector: 'app-accounts',
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './accounts.html',
  styleUrl: './accounts.css',
})
export class Accounts {}
