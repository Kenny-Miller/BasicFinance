import { bootstrapApplication } from '@angular/platform-browser';

import { App } from './app/app';
import { createAppConfig } from './app/app.config';
import { environmentConfigSchema } from './app/environment-config';

void fetch('/environment-config.json')
  .then(resp => resp.json())
  .then(config => {
    const environmentConfig = environmentConfigSchema.parse(config);
    void bootstrapApplication(App, createAppConfig(environmentConfig));
  });
