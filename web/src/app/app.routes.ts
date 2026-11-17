import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    // Lazy loading: the page's code is split into its own file and only downloaded when the route is visited.
    // loadComponent points straight at a component; no feature module with RouterModule.forChild() needed.
    loadComponent: () =>
      import('./wallets/wallet-search/wallet-search').then((m) => m.WalletSearch),
  },
  { path: '**', redirectTo: '' },
];
