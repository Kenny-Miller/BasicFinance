// OAuth library wraps the user profile data in an additional object.
export interface AuthUserProfileResponse {
  info: AuthUserProfile;
}

export interface AuthUserProfile {
  acr: string;
  at_hash: string;
  aud: string;
  auth_time: number;
  azp: string;
  email?: string;
  email_verified?: boolean;
  exp: number;
  family_name?: string;
  given_name?: string;
  iat: number;
  iss: string;
  jti: string;
  name?: string;
  preferred_username: string;
  sid: string;
  sub: string;
  type: string;
  [key: string]: any;
}
