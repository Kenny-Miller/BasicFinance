import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';

export const authGuard: CanActivateFn = (route, state) => {
    const oauthService: OAuthService = inject(OAuthService);
    const router: Router = inject(Router);
    console.log('auth guard called');
    console.log(oauthService);
    if (oauthService.hasValidAccessToken() && oauthService.hasValidIdToken()) {
        //router.navigate(['/user-home']);
        return true;
    } else {
        console.log('no token');
        // router.navigate(['/login']);
        return false;
    }
};