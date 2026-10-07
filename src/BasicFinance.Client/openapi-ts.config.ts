import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: '../../contracts/openapi.json',
  output: 'src/app/core/api/generated',
  responseStyle: 'fields',
  throwOnError: true,
  plugins: ['@hey-api/client-angular', '@hey-api/typescript', '@hey-api/sdk'],
});
