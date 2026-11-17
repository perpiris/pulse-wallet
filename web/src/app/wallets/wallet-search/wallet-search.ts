import { Component, computed, inject, input, linkedSignal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ApiError } from '../../core/api-error';
import { WalletCard } from '../wallet-card/wallet-card';
import { WalletsApi } from '../wallets-api';

const WALLET_ID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// The home page: paste a wallet ID, see the wallet.
//
// No `standalone: true`: every component is standalone by default since v19, and NgModules are legacy.
// `imports` lists what this component's template uses (other components, pipes, directives),
// which is what an NgModule's `declarations`/`imports` used to do for a whole group of components.
//
// No `changeDetection: ChangeDetectionStrategy.OnPush` either: OnPush is the default in v22.
// The view only re-renders when a signal it reads changes, an input changes, or an event fires in it.
@Component({
  selector: 'app-wallet-search',
  imports: [WalletCard],
  templateUrl: './wallet-search.html',
})
export class WalletSearch {
  // inject() replaces constructor injection (`constructor(private walletsApi: WalletsApi)`).
  // It works in field initializers, so the dependencies sit next to the code that uses them.
  private readonly walletsApi = inject(WalletsApi);

  // The ?id=... query parameter, filled in by the router (withComponentInputBinding in app.config.ts).
  // "Create wallet" navigates here with the new wallet's ID, so its card shows straight away.
  readonly id = input<string>();

  // A signal is a value that tells Angular when it changes: read it by calling it (`query()`),
  // change it with `set()` or `update()`. Templates that read a signal re-render when it changes.
  // `protected`: the template can use it (templates count as part of the class), other code can't.
  //
  // linkedSignal: a writable signal that resets whenever its source changes. Typing sets it as usual;
  // a navigation with a new ?id= replaces whatever was typed. A plain signal() couldn't follow the input,
  // a computed() couldn't be typed into.
  protected readonly query = linkedSignal(() => this.id() ?? '');

  // computed() derives a signal from other signals. It recalculates only when `query` changes,
  // and caches the result in between, so the template can read it as often as it likes.
  private readonly walletId = computed(() => {
    const value = this.query().trim();
    return WALLET_ID.test(value) ? value : undefined;
  });

  // Only complain once the input is as long as a GUID, not while someone is still typing one.
  protected readonly invalidId = computed(
    () => this.query().trim().length >= 36 && this.walletId() === undefined,
  );

  // A resource loads async data whenever its `params` signal changes and exposes the result as signals:
  // value(), isLoading(), error(), hasValue(). No subscribe(), no unsubscribe, no loading flags by hand.
  // - `params` returns undefined → the resource stays idle and makes no request.
  // - A new ID while a request is in flight cancels the old request, like RxJS's switchMap.
  // `rxResource` is the variant whose loader returns an Observable, which is what HttpClient gives us.
  protected readonly wallet = rxResource({
    params: () => this.walletId(),
    stream: ({ params }) => this.walletsApi.getById(params),
  });

  // The interceptor turns HTTP failures into ApiError; anything else (a bug in our own code) gets a generic message.
  protected readonly error = computed(() => {
    const error = this.wallet.error();
    if (error === undefined) {
      return undefined;
    }
    return error instanceof ApiError ? error : ApiError.unexpected();
  });
}
