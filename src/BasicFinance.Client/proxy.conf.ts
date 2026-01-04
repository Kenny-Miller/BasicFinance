import { IncomingMessage, ServerResponse } from 'http';
import environmentConfigData from './environment-config-data.ts';

export default {
  '/api/**': {
    target: process.env['services__api__https__0'] || process.env['services__api__http__0'],
    secure: process.env['NODE_ENV'] !== 'development',
    pathRewrite: {
      '^/api': '',
    },
  },
  '/environment-config.json': {
    target: process.env['services__api__https__0'] || process.env['services__api__http__0'],
    secure: false,
    logLevel: 'debug',
    bypass: (req: IncomingMessage, res: ServerResponse, proxyOptions: any) => {
      /**
       * Intercept requet for config file as when running in serve mode, we won't be able to run the
       * post-build script. When deploying @link file://./scripts/generate-environment-config.ts
       * will build the {@link file://./environment-config-data.ts | enviornment json} and place it in the public assets folder.
       */
      res.end(JSON.stringify(environmentConfigData));
      return true;
    },
  },
};
