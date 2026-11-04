import { HttpClient } from '@angular/common/http';
import { Injectable, inject, resource } from '@angular/core';

import { getApiMyInstitutions } from '../api/generated/sdk.gen';

export type { InstitutionDto as Institution } from '../api/generated/types.gen';

@Injectable({
  providedIn: 'root',
})
export class InstitutionClient {
  private readonly httpClient = inject(HttpClient);

  /**
   * Provides the user's active institutions resource (post-authentication).
   * The calling service stores the returned resource and exposes its
   * value/loading/error; refetches are the service's job via the resource's
   * `reload()`.
   */
  myInstitutions() {
    return resource({
      loader: async () => {
        const response = await getApiMyInstitutions({
          httpClient: this.httpClient,
          throwOnError: true,
        });
        return response.data;
      },
    });
  }
}
