import { Routes } from '@angular/router';
import { Account } from './account/account';
import { ManageSpreadsheets } from './manage-spreadsheets/manage-spreadsheets';

export const settingsRoutes: Routes = [
  { path: 'spreadsheets', component: ManageSpreadsheets },
  { path: 'account', component: Account },
  { path: '**', redirectTo: 'spreadsheets' },
];
