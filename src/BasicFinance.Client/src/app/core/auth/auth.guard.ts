import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';

export const authGuard: CanActivateFn = (_route, _state) => {
  const oauthService: OAuthService = inject(OAuthService);
  if (oauthService.hasValidAccessToken() && oauthService.hasValidIdToken()) {
    return true;
  } else {
    return false;
  }
};
