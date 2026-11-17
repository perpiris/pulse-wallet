import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

// Starts the app from a root component and a config object.
// Before standalone components this was `platformBrowserDynamic().bootstrapModule(AppModule)`.
bootstrapApplication(App, appConfig).catch((err) => console.error(err));
