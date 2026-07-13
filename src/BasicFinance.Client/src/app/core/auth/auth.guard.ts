import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';

export const authGuard: CanActivateFn = (route, state) => {
  const oauthService: OAuthService = inject(OAuthService);
  if (oauthService.hasValidAccessToken() && oauthService.hasValidIdToken()) {
    return true;
  } else {
    return false;
  }
};
