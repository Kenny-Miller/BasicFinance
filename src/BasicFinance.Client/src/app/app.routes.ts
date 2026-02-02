import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { Accounts } from './features/accounts/accounts';
import { Home } from './features/home/home';
import { Recurring } from './features/recurring/recurring';
import { Settings } from './features/settings/settings';
import { Spending } from './features/spending/spending';
import { Transactions } from './features/transactions/transactions';

export const routes: Routes = [
  { path: 'accounts', component: Accounts, canActivate: [authGuard] },
  { path: 'recurring', component: Recurring, canActivate: [authGuard] },
  { path: 'transactions', component: Transactions, canActivate: [authGuard] },
  { path: 'spending', component: Spending, canActivate: [authGuard] },
  {
    path: 'settings',
    component: Settings,
    canActivate: [authGuard],
    loadChildren: () => import('./features/settings/settings.routes').then((m) => m.settingsRoutes),
  },
  { path: '**', component: Home, canActivate: [authGuard] },
];
