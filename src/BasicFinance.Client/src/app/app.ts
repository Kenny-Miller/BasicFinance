import { Component, inject, linkedSignal, OnDestroy, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MenuItem, PrimeIcons } from 'primeng/api';
import { Menu } from 'primeng/menu';
import { Toolbar } from 'primeng/toolbar';
import { Card } from 'primeng/card';
import { Drawer } from 'primeng/drawer';
import { DividerModule } from 'primeng/divider';
import { Button } from 'primeng/button';
import { LayoutService } from './core/layout/layout.service';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Menu, Toolbar, Drawer, Button, CommonModule, DividerModule, Card],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {

  protected readonly title = signal('BasicFinance.Client');

  private readonly layoutService = inject(LayoutService)
  readonly isMobile = this.layoutService.isMobile;

  readonly isDrawerOpen = linkedSignal({ source: this.isMobile, computation: () => false });

  readonly navigationItems: MenuItem[] = [
    { label: 'Dashboard', icon: PrimeIcons.TH_LARGE },
    { label: 'Accounts', icon: PrimeIcons.USERS },
    { label: 'Recurring', icon: PrimeIcons.CLOCK },
    { label: 'Transactions', icon: PrimeIcons.RECEIPT },
    { label: 'Spending', icon: PrimeIcons.MONEY_BILL }
  ];

  readonly settingItem: MenuItem = { label: 'Settings', icon: PrimeIcons.COG }
}


