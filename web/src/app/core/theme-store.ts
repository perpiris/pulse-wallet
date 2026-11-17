import { DOCUMENT } from '@angular/common';
import { computed, effect, inject, Service, signal } from '@angular/core';

type Theme = 'light' | 'dark';

const STORAGE_KEY = 'pulsewallet-theme';

// App-wide UI state that isn't server data: which theme is on.
// A singleton service holding signals is the simplest "store" in modern Angular; no NgRx needed for this.
@Service()
export class ThemeStore {
  // DOCUMENT instead of the global `document`: works in tests and with server-side rendering, where there's no browser.
  private readonly document = inject(DOCUMENT);

  // Starts from what the script in index.html already decided, so Angular and the page agree from the first render.
  private readonly theme = signal<Theme>(
    this.document.documentElement.classList.contains('dark') ? 'dark' : 'light',
  );

  // Exposed read-only: components can read the theme but only change it through toggle().
  readonly isDark = computed(() => this.theme() === 'dark');

  constructor() {
    // effect() runs now and again whenever a signal it reads changes. It's for syncing signals to the
    // outside world (here: the DOM). Don't use it to set other signals; that's what computed() is for.
    effect(() => {
      this.document.documentElement.classList.toggle('dark', this.isDark());
    });
  }

  toggle(): void {
    const next: Theme = this.isDark() ? 'light' : 'dark';
    this.theme.set(next);

    // Saved only when the user chooses, so until then the OS setting keeps deciding.
    // localStorage throws in some private-browsing modes; the toggle still works, it just isn't remembered.
    try {
      localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // Not remembered, nothing else to do.
    }
  }
}
