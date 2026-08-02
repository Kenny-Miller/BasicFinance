import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { Account } from './features//account/account';
import { Home } from './features/home/home';
import { Settings } from './features/settings/settings';
import { Spending } from './features/spending/spending';
import { Transactions } from './features/transactions/transactions';

export const routes: Routes = [
  {
    path: 'Transactions',
    component: Transactions,
    canActivate: [authGuard],
    title: 'Transactions',
  },
  { path: 'Spending', component: Spending, canActivate: [authGuard], title: 'Spending' },
  {
    path: 'Settings',
    component: Settings,
    canActivate: [authGuard],
    loadChildren: () => import('./features/settings/settings.routes').then((m) => m.settingsRoutes),
    title: 'Settings',
  },
  {
    path: 'Accounts/:id',
    component: Account,
    canActivate: [authGuard],
    title: 'Account',
  },
  { path: '**', component: Home, canActivate: [authGuard], title: 'Home' },
];
