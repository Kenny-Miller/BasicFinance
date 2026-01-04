import { InjectionToken } from '@angular/core';
import { z } from 'zod';

export const environmentConfigSchema = z.object({
  basicFinanceApi: z.string().min(1),
  openIdAuthority: z.string().min(1),
  openIdClientId: z.string().min(1),
});

export type EnvironmentConfig = z.infer<typeof environmentConfigSchema>;
export const ENVIRONMENT_CONFIG = new InjectionToken<EnvironmentConfig>(
  'Environment_Configuration',
);
