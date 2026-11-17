# PulseWallet web

The Angular client for the PulseWallet API. How to run it is in the main [README](../README.md#working-on-the-frontend).

## Layout

```
src/
  main.ts                 starts the app: bootstrapApplication(App, appConfig)
  styles.css              global styles: Tailwind
  app/
    app.ts / app.html     root component: page shell + <router-outlet />
    app.config.ts         app-wide providers (router, HttpClient, interceptors)
    app.routes.ts         routes, lazy loaded
    core/                 app-wide pieces used by every feature
      api-error.ts, problem-details.interceptor.ts   one error type for every failed request
      theme-store.ts, theme-toggle/                   light/dark theme (index.html sets it before Angular starts)
    wallets/              one folder per feature
      wallets-api.ts      HTTP calls for wallets
      wallet.models.ts    response types, mirroring the API's C# records
      wallet-search/      a page (routed): owns state, talks to the API
      create-wallet/      a page (routed): a Signal Forms form
      wallet-card/        a presentational component: only displays its input
```

Grouped by feature, not by kind: no top-level `components/`, `services/`, `models/` folders. Everything about wallets sits together.

## Angular 15–17 → 22 cheat sheet

| Then                                                          | Now                                                                                      | Where it's used here                  |
| ------------------------------------------------------------- | ---------------------------------------------------------------------------------------- | ------------------------------------- |
| `NgModule` with `declarations`                                | Standalone components (default, no flag): each lists what it uses in `imports`           | every component                       |
| `standalone: true`                                            | Leave it out, it's the default                                                           |                                       |
| `AppModule` + `bootstrapModule`                               | `bootstrapApplication(App, appConfig)`                                                   | `main.ts`, `app.config.ts`            |
| `HttpClientModule`, `RouterModule.forRoot()`                  | `provideHttpClient()`, `provideRouter()`                                                 | `app.config.ts`                       |
| Lazy feature modules (`loadChildren` + `forChild`)            | `loadComponent` / `loadChildren` pointing at routes                                      | `app.routes.ts`                       |
| `constructor(private x: X)`                                   | `private readonly x = inject(X)`                                                         | `wallets-api.ts`, `wallet-search.ts`  |
| `@Injectable({ providedIn: 'root' })`                         | `@Service()`                                                                             | `wallets-api.ts`                      |
| Class interceptors + `HTTP_INTERCEPTORS`                      | Functions + `withInterceptors([...])`                                                    | `core/problem-details.interceptor.ts` |
| `@Input()` / `@Output()`                                      | `input()`, `input.required()`, `output()`, `model()`                                     | `wallet-card.ts`                      |
| Fields + manual change detection, `BehaviorSubject` for state | `signal()`, `computed()`, `linkedSignal()`                                               | `wallet-search.ts`                    |
| `ActivatedRoute.queryParams.subscribe(...)`                   | `withComponentInputBinding()` + `input()` named like the param                           | `wallet-search.ts` (`?id=`)           |
| `subscribe()` + loading flags + `takeUntil(destroy$)`         | `resource()` / `rxResource()` / `httpResource()`                                         | `wallet-search.ts`                    |
| `*ngIf`, `*ngFor`, `*ngSwitch`                                | `@if`, `@for (…; track …)`, `@switch`                                                    | both templates                        |
| `[ngClass]`, `[ngStyle]`                                      | `[class.x]="…"`, `[style.x]="…"`                                                         | `wallet-search.html`                  |
| zone.js decides when to re-render                             | Zoneless: signals, inputs and events trigger re-renders                                  | the whole app                         |
| `ChangeDetectionStrategy.OnPush` opt-in                       | OnPush is the default                                                                    |                                       |
| `app.component.ts`, `AppComponent`                            | `app.ts`, `App` (same for services: no `.service` suffix)                                | file names                            |
| Karma + Jasmine                                               | Vitest                                                                                   | `npm test`                            |
| Template-driven or reactive forms                             | Signal Forms (`@angular/forms/signals`): `form()`, a schema, `[formField]`, `[formRoot]` | `create-wallet.ts`                    |

## Tooling

- `npm run lint`: ESLint with the Angular rules, including accessibility checks on templates
- `npm run format` / `npm run format:check`: Prettier
- Templates are strictly type-checked at build time (the default in v22, no setting needed), so `wallet().balanse` fails the build
