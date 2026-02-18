import { Component, inject } from '@angular/core';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  template: `
    <button
      (click)="theme.toggle()"
      [attr.aria-label]="theme.isDark() ? 'Switch to light mode' : 'Switch to dark mode'"
      class="relative w-9 h-9 rounded-lg flex items-center justify-center
             text-text-secondary hover:text-text-primary hover:bg-surface-2
             transition-all duration-300 cursor-pointer border-none bg-transparent">
      <!-- Sun icon (shown in dark mode → click to go light) -->
      <svg
        [class]="'absolute h-5 w-5 transition-all duration-300 ' +
                 (theme.isDark() ? 'opacity-100 rotate-0 scale-100' : 'opacity-0 rotate-90 scale-0')"
        fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
        <path stroke-linecap="round" stroke-linejoin="round"
              d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
      </svg>
      <!-- Moon icon (shown in light mode → click to go dark) -->
      <svg
        [class]="'absolute h-5 w-5 transition-all duration-300 ' +
                 (theme.isDark() ? 'opacity-0 -rotate-90 scale-0' : 'opacity-100 rotate-0 scale-100')"
        fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
        <path stroke-linecap="round" stroke-linejoin="round"
              d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
      </svg>
    </button>
  `,
})
export class ThemeToggleComponent {
  protected readonly theme = inject(ThemeService);
}
