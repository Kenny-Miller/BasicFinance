import { Routes } from '@angular/router';
import { Home } from './features/home/home';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '*', component: Home, canActivate: [authGuard] },
];
