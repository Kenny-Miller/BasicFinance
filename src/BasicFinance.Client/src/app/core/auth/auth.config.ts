import { AuthConfig } from 'angular-oauth2-oidc';

export const authConfig: AuthConfig = {
  issuer: 'http://localhost:8080/realms/basic-hub',
  redirectUri: window.location.origin + '/index.html',
  clientId: 'basic-finance-public',
  responseType: 'code',
  scope: 'openid profile email offline_access',
  showDebugInformation: true,
  oidc: true,
  requestAccessToken: true,
  timeoutFactor: 0.01,
  checkOrigin: true,
};

//   config: {
//     url: 'http://localhost:8080',
//     realm: 'basic-hub',
//     clientId: 'basic-finance-public',
//   },
//   initOptions: {
//     onLoad: 'check-sso',
//     silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
//     redirectUri: window.location.origin + '/',
//   },
//   features: [
//     withAutoRefreshToken({
//       onInactivityTimeout: 'logout',
//       sessionTimeout: 1000,
//     }),
//   ],
//   providers: [
//     AutoRefreshTokenService,
//     UserActivityService,
//     {
//       provide: INCLUDE_BEARER_TOKEN_INTERCEPTOR_CONFIG,
//       useValue: [localhostCondition],
//     },
//   ],