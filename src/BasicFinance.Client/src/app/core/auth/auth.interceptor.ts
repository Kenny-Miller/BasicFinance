import { HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';

export function authInterceptor(req: HttpRequest<unknown>, next: HttpHandlerFn) {
  const oauthService = inject(OAuthService);
  const token = oauthService.getAccessToken();

  const reqWithToken = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`,
    },
  });

  return next(reqWithToken);
}
