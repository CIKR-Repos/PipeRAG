import { Component, inject, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink],
  template: `
    <nav class="navbar">
      <a routerLink="/dashboard" class="navbar-brand">
        <span class="logo-icon">⚡</span>
        <span>PipeRAG</span>
      </a>
      <div class="navbar-right">
        <span class="user-email">{{ userEmail() }}</span>
        <span class="tier-badge" [class]="'tier-' + userTier().toLowerCase()">{{ userTier() }}</span>
        <button class="logout-btn" (click)="logout()">Logout</button>
      </div>
    </nav>
  `,
  styles: [`
    .navbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.75rem 2rem;
      background: #1a1a2e;
      color: white;
      position: sticky;
      top: 0;
      z-index: 100;
    }
    .navbar-brand {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 1.25rem;
      font-weight: 700;
      color: white;
      text-decoration: none;
    }
    .logo-icon { font-size: 1.5rem; }
    .navbar-right {
      display: flex;
      align-items: center;
      gap: 1rem;
    }
    .user-email {
      font-size: 0.9rem;
      opacity: 0.8;
    }
    .tier-badge {
      padding: 0.2rem 0.6rem;
      border-radius: 12px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
    }
    .tier-free { background: rgba(255,255,255,0.15); color: white; }
    .tier-pro { background: #0099ff; color: white; }
    .tier-enterprise { background: linear-gradient(135deg, #0099ff, #00d4ff); color: white; }
    .logout-btn {
      background: rgba(255,255,255,0.1);
      color: white;
      border: 1px solid rgba(255,255,255,0.2);
      padding: 0.4rem 1rem;
      border-radius: 8px;
      cursor: pointer;
      font-size: 0.85rem;
      transition: all 0.3s ease;
    }
    .logout-btn:hover {
      background: rgba(255,255,255,0.2);
    }
  `],
})
export class NavbarComponent {
  private auth = inject(AuthService);

  userEmail = computed(() => this.auth.user()?.email ?? '');
  userTier = computed(() => this.auth.userTier());

  logout() {
    this.auth.logout();
  }
}
