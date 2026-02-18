import { Component, inject, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeToggleComponent } from '../theme-toggle/theme-toggle';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, ThemeToggleComponent],
  template: `
    <nav class="sticky top-0 z-50 border-b border-border bg-surface-0/80 backdrop-blur-xl">
      <div class="flex items-center justify-between px-6 py-3">
        <!-- Logo -->
        <a routerLink="/dashboard"
           class="flex items-center gap-2.5 text-xl font-bold text-text-primary no-underline
                  hover:text-accent transition-colors">
          <span class="text-2xl">&#9889;</span>
          <span>PipeRAG</span>
        </a>

        <!-- Desktop nav -->
        <div class="hidden sm:flex items-center gap-3">
          <span class="text-sm text-text-secondary">{{ userEmail() }}</span>
          <a routerLink="/billing"
             class="text-sm text-text-tertiary hover:text-text-primary transition-colors no-underline px-3 py-1.5 rounded-lg hover:bg-surface-2">
            Billing
          </a>
          <app-theme-toggle />
          <span class="px-2.5 py-1 rounded-full text-xs font-semibold uppercase tracking-wider"
                [class]="tierClass()">
            {{ userTier() }}
          </span>
          <button (click)="logout()"
                  class="text-text-secondary hover:text-text-primary border border-border
                         px-4 py-1.5 rounded-lg text-sm cursor-pointer hover:bg-surface-2
                         transition-all duration-200 bg-transparent">
            Logout
          </button>
        </div>

        <!-- Mobile hamburger -->
        <button (click)="menuOpen.set(!menuOpen())"
                class="sm:hidden bg-transparent border-none text-text-primary cursor-pointer p-1"
                [attr.aria-expanded]="menuOpen()"
                aria-label="Toggle menu">
          <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            @if (menuOpen()) {
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
            } @else {
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16" />
            }
          </svg>
        </button>
      </div>

      <!-- Mobile menu panel -->
      @if (menuOpen()) {
        <div class="sm:hidden border-t border-border px-6 py-4 flex flex-col gap-3
                    bg-surface-0/95 backdrop-blur-xl animate-slide-down">
          <span class="text-sm text-text-secondary">{{ userEmail() }}</span>
          <a routerLink="/billing" (click)="menuOpen.set(false)"
             class="text-sm text-text-tertiary hover:text-text-primary transition-colors no-underline">
            Billing
          </a>
          <div class="flex items-center gap-3">
            <app-theme-toggle />
            <span class="px-2.5 py-1 rounded-full text-xs font-semibold uppercase tracking-wider"
                  [class]="tierClass()">
              {{ userTier() }}
            </span>
          </div>
          <button (click)="logout()"
                  class="w-full text-text-secondary hover:text-text-primary border border-border
                         px-4 py-2 rounded-lg text-sm cursor-pointer hover:bg-surface-2
                         transition-all duration-200 bg-transparent">
            Logout
          </button>
        </div>
      }
    </nav>
  `,
})
export class NavbarComponent {
  private auth = inject(AuthService);

  menuOpen = signal(false);
  userEmail = computed(() => this.auth.user()?.email ?? '');
  userTier = computed(() => this.auth.userTier());
  tierClass = computed(() => {
    const t = this.auth.userTier().toLowerCase();
    if (t === 'pro') return 'bg-accent/20 text-accent';
    if (t === 'enterprise') return 'bg-gradient-to-br from-accent to-purple-500 text-white';
    return 'bg-surface-2 text-text-secondary';
  });

  logout() {
    this.auth.logout();
  }
}
