import {
  AutoRefreshTokenService,
  createInterceptorCondition,
  INCLUDE_BEARER_TOKEN_INTERCEPTOR_CONFIG,
  IncludeBearerTokenCondition,
  ProvideKeycloakOptions,
  UserActivityService,
  withAutoRefreshToken,
} from 'keycloak-angular';

const localhostCondition = createInterceptorCondition<IncludeBearerTokenCondition>({
  urlPattern: /^(https:\/\/localhost:7119)(\/.*)?$/i,
});

export const keycloakProvider: ProvideKeycloakOptions = {
  config: {
    url: 'http://localhost:8080',
    realm: 'basic-hub',
    clientId: 'basic-finance-public',
  },
  initOptions: {
    onLoad: 'check-sso',
    silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
    redirectUri: window.location.origin + '/',
  },
  features: [
    withAutoRefreshToken({
      onInactivityTimeout: 'logout',
      sessionTimeout: 1000,
    }),
  ],
  providers: [
    AutoRefreshTokenService,
    UserActivityService,
    {
      provide: INCLUDE_BEARER_TOKEN_INTERCEPTOR_CONFIG,
      useValue: [localhostCondition],
    },
  ],
};
