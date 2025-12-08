import { inject } from "@angular/core";
import { OAuthService } from "angular-oauth2-oidc";
import { authConfig } from "./auth.config";

export const initializeOAuthFn = () => {
    const oauthService = inject(OAuthService);

    oauthService.configure(authConfig);
    oauthService.setupAutomaticSilentRefresh();
    return oauthService.loadDiscoveryDocumentAndLogin();
};