import { MenuItem } from './menu-item';

export interface NavMenuItem extends MenuItem {
  routerLink: string;
}
