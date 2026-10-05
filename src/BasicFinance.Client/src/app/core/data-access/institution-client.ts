import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';

export interface Institution {
  id: number;
  code: string;
  name: string;
  logoUrl: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class InstitutionClient {
  /**
   * Provides the user's active institutions resource (post-authentication).
   * The calling service stores the returned resource and exposes its
   * value/loading/error; refetches are the service's job via the resource's
   * `reload()`.
   */
  myInstitutions() {
    return httpResource<Institution[]>(() => 'api/my/institutions');
  }
}
