import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PageService } from '../../../core/page/page.service';
import { ThemeService } from '../../../core/theme/theme.service';

@Component({
  selector: 'app-account',
  imports: [],
  templateUrl: './account.html',
  styleUrl: './account.css',
})
export class Account {}
