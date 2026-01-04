import { provideHttpClient } from '@angular/common/http';
import {
  ApplicationConfig,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideOAuthClient } from 'angular-oauth2-oidc';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { routes } from './app.routes';
import { initializeOAuthFn } from './core/auth/auth.initializer';
import { ENVIRONMENT_CONFIG, EnvironmentConfig } from './environment-config';

export const createAppConfig = (config: EnvironmentConfig): ApplicationConfig => {
  return {
    providers: [
      provideBrowserGlobalErrorListeners(),
      provideRouter(routes),
      provideHttpClient(),
      provideOAuthClient(),
      provideAppInitializer(initializeOAuthFn),
      provideCharts(withDefaultRegisterables()),
      { provide: ENVIRONMENT_CONFIG, useValue: config },
    ],
  };
};
