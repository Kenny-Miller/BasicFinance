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
