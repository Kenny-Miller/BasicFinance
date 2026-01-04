import { bootstrapApplication } from '@angular/platform-browser';
import { App } from './app/app';
import { createAppConfig } from './app/app.config';
import { environmentConfigSchema } from './app/environment-config';

fetch('/environment-config.json')
  .then((resp) => resp.json())
  .then((config) => {
    const environmentConfig = environmentConfigSchema.parse(config);
    bootstrapApplication(App, createAppConfig(environmentConfig));
  });
