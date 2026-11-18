import { DecimalPipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { ApiError } from '../../core/api-error';
import { AuthStore } from '../../core/auth/auth-store';
import { WalletsApi } from '../wallets-api';

// The home page once signed in: the user's wallets, each linking to its own page.
@Component({
  selector: 'app-my-wallets',
  imports: [DecimalPipe, RouterLink],
  templateUrl: './my-wallets.html',
})
export class MyWallets {
  private readonly walletsApi = inject(WalletsApi);
  protected readonly authStore = inject(AuthStore);

  // No `params`: nothing to react to, so it loads once when the page opens.
  protected readonly wallets = rxResource({
    stream: () => this.walletsApi.list(),
  });

  protected readonly error = computed(() => {
    const error = this.wallets.error();
    return error === undefined ? undefined : ApiError.of(error);
  });
}
