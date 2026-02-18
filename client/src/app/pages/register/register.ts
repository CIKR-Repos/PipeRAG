import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div class="w-full max-w-sm bg-white rounded-2xl shadow-lg p-8">
        <div class="text-center mb-8">
          <div class="text-3xl mb-2">⚡</div>
          <h1 class="text-2xl font-bold text-dark">Register for PipeRAG</h1>
        </div>
        @if (error()) {
          <div class="bg-red-50 text-red-600 text-sm px-4 py-3 rounded-lg mb-4">{{ error() }}</div>
        }
        <form (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-1.5">Display Name</label>
            <input type="text" [(ngModel)]="displayName" name="displayName" required
                   class="w-full px-3.5 py-2.5 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20 transition-all duration-300" />
          </div>
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-1.5">Email</label>
            <input type="email" [(ngModel)]="email" name="email" required
                   class="w-full px-3.5 py-2.5 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20 transition-all duration-300" />
          </div>
          <div>
            <label class="block text-sm font-semibold text-gray-700 mb-1.5">Password</label>
            <input type="password" [(ngModel)]="password" name="password" required minlength="8"
                   class="w-full px-3.5 py-2.5 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20 transition-all duration-300" />
          </div>
          <button type="submit" [disabled]="loading()"
                  class="w-full py-3 bg-gradient-to-br from-accent to-cyan text-white font-semibold rounded-lg cursor-pointer hover:-translate-y-0.5 hover:shadow-lg transition-all duration-300 border-none disabled:opacity-50 disabled:cursor-not-allowed">
            {{ loading() ? 'Registering...' : 'Register' }}
          </button>
        </form>
        <p class="text-center text-sm text-gray-500 mt-6">
          Already have an account?
          <a routerLink="/login" class="text-accent font-semibold hover:underline">Login</a>
        </p>
      </div>
    </div>
  `,
})
export class RegisterComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  displayName = '';
  email = '';
  password = '';
  loading = signal(false);
  error = signal('');

  async onSubmit() {
    this.loading.set(true);
    this.error.set('');
    try {
      await this.auth.register(this.email, this.password, this.displayName);
      this.router.navigate(['/dashboard']);
    } catch (e: any) {
      this.error.set(e?.error?.error || 'Registration failed');
    } finally {
      this.loading.set(false);
    }
  }
}
