import { isPlatformBrowser, JsonPipe, DatePipe, CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, computed, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { Panel } from "primeng/panel";
import { Card } from "primeng/card";
import { DividerModule } from 'primeng/divider';
import { TableModule } from "primeng/table";
import { ChartModule } from 'primeng/chart';
import { AuthUserProfile, AuthUserProfileResponse } from '../../core/auth/auth-userprofile';

@Component({
  selector: 'app-home',
  imports: [JsonPipe, DatePipe, Panel, ChartModule, Card, CommonModule, TableModule, DividerModule],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  oauthService = inject(OAuthService);
  user = signal<AuthUserProfile | null>(null);
  currentDate = new Date();
  welcomeText = computed(() => this.currentDate.getHours() < 12 ? `Good Morning ${this.user()?.given_name}` : `Good Afternoon ${this.user()?.given_name}`)

  data: any;

  options: any;

  platformId = inject(PLATFORM_ID);

  products = [
    {
      name: 'test',
      category: 'test',
      quantity: 2,
    }
  ]
  constructor(private cd: ChangeDetectorRef) { }

  async ngOnInit() {
    const response = await this.oauthService.loadUserProfile() as AuthUserProfileResponse;
    console.log(response.info)
    this.user.set(response.info);
    this.initChart();
  }

  initChart() {
    if (isPlatformBrowser(this.platformId)) {
      const documentStyle = getComputedStyle(document.documentElement);
      const textColor = documentStyle.getPropertyValue('--p-text-color');
      const textColorSecondary = documentStyle.getPropertyValue('--p-text-muted-color');
      const surfaceBorder = documentStyle.getPropertyValue('--p-content-border-color');

      this.data = {
        labels: ['January', 'February', 'March', 'April', 'May', 'June', 'July'],
        datasets: [
          {
            label: 'First Dataset',
            data: [65, 59, 80, 81, 56, 55, 40],
            fill: false,
            tension: 0.4,
            borderColor: documentStyle.getPropertyValue('--p-cyan-500')
          },
          {
            label: 'Second Dataset',
            data: [28, 48, 40, 19, 86, 27, 90],
            fill: false,
            borderDash: [5, 5],
            tension: 0.4,
            borderColor: documentStyle.getPropertyValue('--p-orange-500')
          },
          {
            label: 'Third Dataset',
            data: [12, 51, 62, 33, 21, 62, 45],
            fill: true,
            borderColor: documentStyle.getPropertyValue('--p-gray-500'),
            tension: 0.4,
            backgroundColor: 'rgba(107, 114, 128, 0.2)'
          }
        ]
      };

      this.options = {
        maintainAspectRatio: false,
        aspectRatio: 1,
        plugins: {
          legend: {
            labels: {
              color: textColor
            }
          }
        },
        scales: {
          x: {
            ticks: {
              color: textColorSecondary
            },
            grid: {
              color: surfaceBorder
            }
          },
          y: {
            ticks: {
              color: textColorSecondary
            },
            grid: {
              color: surfaceBorder
            }
          }
        }
      };
      this.cd.markForCheck();
    }
  }
}