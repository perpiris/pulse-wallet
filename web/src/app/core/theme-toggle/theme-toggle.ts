import { Component, inject } from '@angular/core';
import { ThemeStore } from '../theme-store';

@Component({
  selector: 'app-theme-toggle',
  templateUrl: './theme-toggle.html',
})
export class ThemeToggle {
  protected readonly themeStore = inject(ThemeStore);
}
