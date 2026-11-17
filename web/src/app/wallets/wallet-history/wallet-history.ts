import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { form, FormField, validate } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiError } from '../../core/api-error';
import { LedgerEntryRow } from '../ledger-entry-row/ledger-entry-row';
import { LedgerEntriesQuery, LedgerEntryDirection } from '../wallet.models';
import { WalletsApi } from '../wallets-api';

interface HistoryFilters {
  direction: '' | LedgerEntryDirection;
  from: string;
  to: string;
}

const NO_FILTERS: HistoryFilters = { direction: '', from: '', to: '' };

// A wallet's full history: filters at the top, 10 entries per page, "Load more" for the next page.
@Component({
  selector: 'app-wallet-history',
  imports: [DecimalPipe, FormField, RouterLink, LedgerEntryRow],
  templateUrl: './wallet-history.html',
})
export class WalletHistory {
  private readonly walletsApi = inject(WalletsApi);

  // The :id route parameter (route 'wallets/:id/history'), delivered by withComponentInputBinding.
  // `required`: the route can't match without an id, so it's always there.
  readonly id = input.required<string>();

  // For the header (username, balance). A separate resource from the entries: it doesn't reload when filters change.
  protected readonly wallet = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.walletsApi.getById(params),
  });

  protected readonly directionOptions: readonly {
    value: HistoryFilters['direction'];
    label: string;
  }[] = [
    { value: '', label: 'All' },
    { value: 'Incoming', label: 'In' },
    { value: 'Outgoing', label: 'Out' },
  ];

  // The filters are a small Signal Form: the inputs write into this signal, the list reacts to it.
  protected readonly filters = signal<HistoryFilters>(NO_FILTERS);

  protected readonly filterForm = form(this.filters, (path) => {
    // A custom rule: validate() gets the field's context and returns an error or undefined.
    // valueOf(path.from) reads another field, so this rule re-runs when either date changes.
    validate(path.to, ({ value, valueOf }) => {
      const from = valueOf(path.from);
      const to = value();
      return from && to && from > to
        ? { kind: 'dateRange', message: "'To' can't be before 'From'." }
        : undefined;
    });
  });

  protected readonly hasFilters = computed(() => {
    const { direction, from, to } = this.filters();
    return direction !== '' || from !== '' || to !== '';
  });

  // What gets sent to the API: undefined while the filters are invalid, so no request goes out.
  // "yyyy-MM-dd" from <input type="date"> is exactly what the API's DateOnly expects; '' means "not set".
  private readonly query = computed<LedgerEntriesQuery | undefined>(() => {
    if (this.filterForm().invalid()) {
      return undefined;
    }
    const { direction, from, to } = this.filters();
    return { direction: direction || undefined, from: from || undefined, to: to || undefined };
  });

  // The first page. New filters → a new query → the resource fetches page one again (and cancels a request in flight).
  // Everything the loader depends on goes in `params`: only signals read there trigger a reload,
  // so reading this.id() inside `stream` instead would miss a change of wallet.
  protected readonly firstPage = rxResource({
    params: () => {
      const query = this.query();
      return query === undefined ? undefined : { id: this.id(), query };
    },
    stream: ({ params }) => this.walletsApi.getLedgerEntries(params.id, params.query),
  });

  // Everything loaded so far: page one plus whatever "Load more" appended.
  // linkedSignal resets to page one whenever the first page changes (new filters), and in between
  // loadMore() can append to it with update(). A computed() couldn't be appended to.
  // hasValue() first: reading value() of a resource in the error state throws.
  protected readonly entries = linkedSignal(() =>
    this.firstPage.hasValue() ? this.firstPage.value().items : [],
  );

  private readonly nextCursor = linkedSignal(() =>
    this.firstPage.hasValue() ? this.firstPage.value().nextCursor : null,
  );

  protected readonly hasMore = computed(() => this.nextCursor() !== null);

  protected readonly loadingMore = signal(false);

  // The long form of linkedSignal: `source` is what it watches, `computation` the value it resets to.
  // Here: forget a "Load more" error as soon as the filters change.
  protected readonly loadMoreError = linkedSignal<
    LedgerEntriesQuery | undefined,
    ApiError | undefined
  >({
    source: this.query,
    computation: () => undefined,
  });

  protected readonly firstPageError = computed(() => {
    const error = this.firstPage.error();
    return error === undefined ? undefined : ApiError.of(error);
  });

  protected async loadMore(): Promise<void> {
    const query = this.query();
    const before = this.nextCursor();
    if (query === undefined || before === null) {
      return;
    }

    this.loadingMore.set(true);
    try {
      const page = await firstValueFrom(
        this.walletsApi.getLedgerEntries(this.id(), { ...query, before }),
      );
      // The filters changed while this page was loading: it belongs to the old list, so drop it.
      // (The resource cancels its own outdated requests; this one is ours to handle.)
      if (query !== this.query()) {
        return;
      }
      this.entries.update((entries) => [...entries, ...page.items]);
      this.nextCursor.set(page.nextCursor);
    } catch (error) {
      this.loadMoreError.set(ApiError.of(error));
    } finally {
      this.loadingMore.set(false);
    }
  }

  protected clearFilters(): void {
    this.filters.set(NO_FILTERS);
  }
}
