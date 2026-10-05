import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: '../../contracts/openapi.json',
  output: 'src/app/core/api/generated',
  plugins: ['@hey-api/client-angular', '@hey-api/typescript', '@hey-api/sdk', 'zod'],
});
