import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <h1>Login to PipeRAG</h1>
      @if (error()) {
        <div class="error">{{ error() }}</div>
      }
      <form (ngSubmit)="onSubmit()">
        <label>Email</label>
        <input type="email" [(ngModel)]="email" name="email" required />
        <label>Password</label>
        <input type="password" [(ngModel)]="password" name="password" required />
        <button type="submit" [disabled]="loading()">
          {{ loading() ? 'Logging in...' : 'Login' }}
        </button>
      </form>
      <p>Don't have an account? <a routerLink="/register">Register</a></p>
    </div>
  `,
  styles: [`
    .auth-container { max-width: 400px; margin: 4rem auto; padding: 2rem; }
    .error { color: red; margin-bottom: 1rem; }
    form { display: flex; flex-direction: column; gap: 0.5rem; }
    input { padding: 0.5rem; border: 1px solid #ccc; border-radius: 4px; }
    button { padding: 0.75rem; background: #1976d2; color: white; border: none; border-radius: 4px; cursor: pointer; }
    button:disabled { opacity: 0.6; }
  `],
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  loading = signal(false);
  error = signal('');

  async onSubmit() {
    this.loading.set(true);
    this.error.set('');
    try {
      await this.auth.login(this.email, this.password);
      this.router.navigate(['/dashboard']);
    } catch (e: any) {
      this.error.set(e?.error?.error || 'Login failed');
    } finally {
      this.loading.set(false);
    }
  }
}
