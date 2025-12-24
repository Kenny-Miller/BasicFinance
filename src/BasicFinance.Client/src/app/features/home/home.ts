import { JsonPipe, DatePipe, CommonModule } from '@angular/common';
import { AfterViewInit, Component, computed, inject, OnInit, signal, ViewChild } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { AuthUserProfile, AuthUserProfileResponse } from '../../core/auth/auth-userprofile';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { ChartConfiguration, ChartData, ChartEvent, ChartOptions, ChartType } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import ChartDeferred from 'chartjs-plugin-deferred';
import 'chartjs-adapter-date-fns';

@Component({
  selector: 'app-home',
  imports: [JsonPipe, DatePipe, CommonModule, HlmCardImports, BaseChartDirective,],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit, AfterViewInit {
  @ViewChild(BaseChartDirective) chart?: BaseChartDirective;

  displayChart = signal(false);



  oauthService = inject(OAuthService);
  user = signal<AuthUserProfile | null>(null);
  currentDate = new Date();
  welcomeText = computed(() => this.currentDate.getHours() < 12 ? `Good Morning ${this.user()?.given_name}` : `Good Afternoon ${this.user()?.given_name}`)

  async ngOnInit() {


    const response = await this.oauthService.loadUserProfile() as AuthUserProfileResponse;
    this.user.set(response.info);
  }

  ngAfterViewInit(): void {
    // Chart doesn't play nicely in flex box as it will overflow slightly till the window resizes
    // This in conjuction with the deferred plugin still allows a nice loading
    this.chart?.chart?.resize();
    this.displayChart.set(true)
  }

  spendActivityChartData: ChartData<'line'> = {
    datasets: [
      {
        data: [65, 59, 80, 81, 56, 55, 40],
        label: 'Income',
        backgroundColor: 'rgba(148,159,177,0.2)',
        borderColor: 'rgba(148,159,177,1)',
        pointBackgroundColor: 'rgba(148,159,177,1)',
        pointBorderColor: '#fff',
        pointHoverBackgroundColor: '#fff',
        pointHoverBorderColor: 'rgba(148,159,177,0.8)',
        fill: 'origin',
      },
      {
        data: [28, 48, 40, 19, 86, 27, 90],
        label: 'Spend',
        backgroundColor: "var(--color-blue-500)",
        borderColor: 'rgba(77,83,96,1)',
        pointBackgroundColor: 'rgba(77,83,96,1)',
        pointBorderColor: '#fff',
        pointHoverBackgroundColor: '#fff',
        pointHoverBorderColor: 'rgba(77,83,96,1)',
        fill: 'origin',
      },
    ],
    labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
  };

  incomeChartData: ChartData<'line', { x: Date, y: number }[]> = {

    datasets: [
      {
        data: [
          { x: new Date('2025-01-01T10:00:00'), y: 50 },
          { x: new Date('2025-01-01T10:00:00'), y: 50 },
          { x: new Date('2025-01-02T12:00:00'), y: 65 },
          { x: new Date('2025-01-03T12:00:00'), y: 40 },
          { x: new Date('2025-01-04T16:00:00'), y: 81 }]
      }
    ]
  }

  chartPlugins = [ChartDeferred]

  spendActivityChartOptions: ChartOptions<'line'> = {
    elements: {
      line: {
        tension: 0.5,
      },
    },
    responsive: true,
    maintainAspectRatio: false,
    aspectRatio: 2,
    plugins: {
      deferred: { delay: 500 },
      legend: { display: true },
      tooltip: {
        mode: 'index',
        intersect: false // Set to false to show the tooltip when near a point, not just on direct intersection
      }
    },
  }

  incomeChartOptions: ChartOptions<'line'> = {
    scales: {
      x: {
        type: 'time',
        time: {
          unit: 'day',
        },
        display: false
      },
      y: {
        display: false
      }
    },
    elements: {
      line: {
        tension: 0.5,
      },
    },
    responsive: true,
    maintainAspectRatio: false,
    aspectRatio: 3,
    plugins: {
      deferred: { delay: 500 },
      legend: { display: false },
      tooltip: {
        enabled: false
      }
    },
  }
}