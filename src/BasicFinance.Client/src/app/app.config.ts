import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { includeBearerTokenInterceptor, provideKeycloak } from 'keycloak-angular';
import { routes } from './app.routes';
import { keycloakProvider } from './keycloak.config';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(),
    provideKeycloak(keycloakProvider),
    provideHttpClient(withInterceptors([includeBearerTokenInterceptor])),
  ],
};
