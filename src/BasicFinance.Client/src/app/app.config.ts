import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideOAuthClient } from 'angular-oauth2-oidc';
import { BarChart, LineChart } from 'echarts/charts';
import { GridComponent, LegendComponent, TooltipComponent } from 'echarts/components';
import * as echarts from 'echarts/core';
import { CanvasRenderer } from 'echarts/renderers';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { provideEchartsCore } from 'ngx-echarts';
import { routes } from './app.routes';
import { initializeOAuthFn } from './core/auth/auth.initializer';
import { authInterceptor } from './core/auth/auth.interceptor';
import { ENVIRONMENT_CONFIG, EnvironmentConfig } from './environment-config';
echarts.use([
  BarChart,
  GridComponent,
  LegendComponent,
  CanvasRenderer,
  TooltipComponent,
  LineChart,
]);

export const createAppConfig = (config: EnvironmentConfig): ApplicationConfig => {
  return {
    providers: [
      provideBrowserGlobalErrorListeners(),
      provideRouter(routes),
      provideHttpClient(withInterceptors([authInterceptor])),
      provideOAuthClient(),
      provideAppInitializer(initializeOAuthFn),
      provideCharts(withDefaultRegisterables()),
      provideEchartsCore({ echarts }),
      { provide: ENVIRONMENT_CONFIG, useValue: config },
    ],
  };
};
