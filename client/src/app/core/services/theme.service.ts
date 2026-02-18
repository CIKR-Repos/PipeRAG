import { Injectable, signal, computed, effect } from '@angular/core';

export type Theme = 'light' | 'dark' | 'system';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'piperag-theme';

  readonly preference = signal<Theme>(this.loadPreference());
  private readonly systemDark = signal(this.detectSystemDark());

  readonly effectiveTheme = computed<'light' | 'dark'>(() => {
    const pref = this.preference();
    if (pref === 'system') return this.systemDark() ? 'dark' : 'light';
    return pref;
  });

  readonly isDark = computed(() => this.effectiveTheme() === 'dark');

  constructor() {
    // Listen for OS theme changes
    if (typeof window !== 'undefined') {
      const mq = window.matchMedia('(prefers-color-scheme: dark)');
      mq.addEventListener('change', (e) => this.systemDark.set(e.matches));
    }

    // Apply theme class to <html> whenever effectiveTheme changes
    effect(() => {
      if (typeof document === 'undefined') return;
      const theme = this.effectiveTheme();
      const html = document.documentElement;
      html.classList.toggle('dark', theme === 'dark');
      html.classList.toggle('light', theme === 'light');
    });

    // Persist preference
    effect(() => {
      if (typeof localStorage === 'undefined') return;
      localStorage.setItem(this.STORAGE_KEY, this.preference());
    });
  }

  toggle(): void {
    const current = this.effectiveTheme();
    this.preference.set(current === 'dark' ? 'light' : 'dark');
  }

  setTheme(theme: Theme): void {
    this.preference.set(theme);
  }

  private loadPreference(): Theme {
    if (typeof localStorage === 'undefined') return 'dark';
    const stored = localStorage.getItem(this.STORAGE_KEY);
    if (stored === 'light' || stored === 'dark' || stored === 'system') return stored;
    return 'dark';
  }

  private detectSystemDark(): boolean {
    if (typeof window === 'undefined') return true;
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  }
}
