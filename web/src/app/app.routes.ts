import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guards';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'wallets' },
  {
    path: 'login',
    canActivate: [guestGuard],
    // Lazy loading: the page's code is split into its own file and only downloaded when the route is visited.
    // loadComponent points straight at a component; no feature module with RouterModule.forChild() needed.
    loadComponent: () => import('./auth/login/login').then((m) => m.Login),
    // `title` sets the browser tab's title on navigation.
    title: 'Sign in · PulseWallet',
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./auth/register/register').then((m) => m.Register),
    title: 'Create an account · PulseWallet',
  },
  {
    // A parent route with no component of its own: it exists to put one guard in front of every wallet page.
    // Its children render in the same <router-outlet /> and their paths are relative to "wallets".
    path: 'wallets',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./wallets/my-wallets/my-wallets').then((m) => m.MyWallets),
        title: 'My wallets · PulseWallet',
      },
      {
        // Before ':id', so "new" isn't taken for a wallet id: routes match in order.
        path: 'new',
        loadComponent: () =>
          import('./wallets/create-wallet/create-wallet').then((m) => m.CreateWallet),
        title: 'Create a wallet · PulseWallet',
      },
      {
        // :id is a route parameter; the component receives it as its `id` input.
        path: ':id',
        loadComponent: () => import('./wallets/wallet-page/wallet-page').then((m) => m.WalletPage),
        title: 'Wallet · PulseWallet',
      },
      {
        path: ':id/history',
        loadComponent: () =>
          import('./wallets/wallet-history/wallet-history').then((m) => m.WalletHistory),
        title: 'History · PulseWallet',
      },
    ],
  },
  { path: '**', redirectTo: 'wallets' },
];
