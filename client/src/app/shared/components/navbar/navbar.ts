import { Component, inject, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink],
  template: `
    <nav class="flex items-center justify-between px-6 py-3 bg-dark text-white sticky top-0 z-50">
      <a routerLink="/dashboard" class="flex items-center gap-2 text-xl font-bold text-white no-underline">
        <span class="text-2xl">⚡</span>
        <span>PipeRAG</span>
      </a>
      <div class="flex items-center gap-4">
        <span class="text-sm text-white/80 hidden sm:inline">{{ userEmail() }}</span>
        <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold uppercase"
              [class]="tierClass()">
          {{ userTier() }}
        </span>
        <button (click)="logout()"
                class="bg-white/10 text-white border border-white/20 px-4 py-1.5 rounded-lg text-sm cursor-pointer hover:bg-white/20 transition-all duration-300">
          Logout
        </button>
      </div>
    </nav>
  `,
})
export class NavbarComponent {
  private auth = inject(AuthService);

  userEmail = computed(() => this.auth.user()?.email ?? '');
  userTier = computed(() => this.auth.userTier());
  tierClass = computed(() => {
    const t = this.auth.userTier().toLowerCase();
    if (t === 'pro') return 'bg-accent text-white';
    if (t === 'enterprise') return 'bg-gradient-to-br from-accent to-cyan text-white';
    return 'bg-white/15 text-white';
  });

  logout() {
    this.auth.logout();
  }
}
