import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { problemDetailsInterceptor } from './core/problem-details.interceptor';

// App-wide providers: what AppModule's `imports` and `providers` used to hold.
// Features are switched on with provideX() functions instead of importing modules
// (provideHttpClient() replaces HttpClientModule, provideRouter() replaces RouterModule.forRoot()).
//
// Zoneless: there's no zone.js in this app (the default since v21). Angular no longer patches every
// setTimeout, promise and event to guess when to re-render; signals tell it exactly what changed.
export const appConfig: ApplicationConfig = {
  providers: [
    // Sends errors that escape everywhere else (unhandled promise rejections, errors in event handlers)
    // to Angular's ErrorHandler, so they're logged in one place.
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // withFetch(): HttpClient uses the browser's fetch() instead of the older XMLHttpRequest.
    // withInterceptors(): interceptors are plain functions now, not classes implementing HttpInterceptor.
    provideHttpClient(withFetch(), withInterceptors([problemDetailsInterceptor])),
  ],
};
