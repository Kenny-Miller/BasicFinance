// @ts-check
import angular from 'angular-eslint';

const templateConfigs = [
  ...angular.configs.templateRecommended,
  ...angular.configs.templateAccessibility,
];

export default [
  {
    ignores: [
      '**/node_modules/**',
      '**/dist/**',
      '**/coverage/**',
      '**/.angular/**',
      'src/BasicFinance.Client/libs/**',
      'src/BasicFinance.Client/src/app/core/api/generated/**',
    ],
  },
  ...templateConfigs.map(config => ({ ...config, files: ['**/*.html'] })),
];
