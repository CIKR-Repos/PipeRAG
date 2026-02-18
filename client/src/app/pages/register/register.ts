import { Component, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  styles: [`
    @keyframes reveal {
      from { opacity: 0; transform: translateY(20px); }
      to { opacity: 1; transform: translateY(0); }
    }
    @keyframes float-a {
      0%, 100% { transform: translate(0, 0) scale(1); }
      33% { transform: translate(25px, -18px) scale(1.04); }
      66% { transform: translate(-18px, 12px) scale(0.96); }
    }
    @keyframes float-b {
      0%, 100% { transform: translate(0, 0) scale(1); }
      50% { transform: translate(-30px, 20px) scale(1.06); }
    }
    .orb-a { animation: float-a 22s ease-in-out infinite; }
    .orb-b { animation: float-b 28s ease-in-out infinite; }
    .reveal { animation: reveal 0.6s cubic-bezier(0.16, 1, 0.3, 1) both; }
    .reveal-d1 { animation-delay: 0.08s; }
    .reveal-d2 { animation-delay: 0.16s; }
    .reveal-d3 { animation-delay: 0.24s; }
    .reveal-d4 { animation-delay: 0.32s; }
    .reveal-d5 { animation-delay: 0.40s; }
    .reveal-d6 { animation-delay: 0.48s; }
  `],
  template: `
    <div class="min-h-screen flex bg-surface-1">

      <!-- Left: Branding Panel -->
      <div class="hidden lg:flex lg:w-1/2 bg-surface-0 flex-col items-center justify-center
                  relative overflow-hidden">
        <!-- Gradient orbs -->
        <div class="absolute top-[15%] left-[20%] w-[360px] h-[360px] rounded-full
                    bg-accent/12 blur-[100px] orb-a pointer-events-none"></div>
        <div class="absolute bottom-[15%] right-[15%] w-[280px] h-[280px] rounded-full
                    bg-cyan/8 blur-[80px] orb-b pointer-events-none"></div>

        <!-- Dot grid -->
        <div class="absolute inset-0 opacity-[0.03]"
             style="background-image: radial-gradient(circle, currentColor 1px, transparent 1px); background-size: 28px 28px;">
        </div>

        <!-- Brand content -->
        <div class="relative z-10 text-center px-12 max-w-md">
          <div class="text-5xl mb-6">&#9889;</div>
          <h2 class="text-3xl font-bold text-text-primary tracking-tight mb-4">PipeRAG</h2>
          <p class="text-text-secondary leading-relaxed">
            Join thousands of teams building AI-powered chatbots from their documents.
            No code required.
          </p>
          <div class="mt-10 grid grid-cols-3 gap-4">
            <div class="bg-surface-2/50 border border-border/50 rounded-xl p-4 text-center">
              <div class="text-lg font-bold text-text-primary">PDF</div>
              <div class="text-xs text-text-tertiary mt-0.5">Supported</div>
            </div>
            <div class="bg-surface-2/50 border border-border/50 rounded-xl p-4 text-center">
              <div class="text-lg font-bold text-text-primary">DOCX</div>
              <div class="text-xs text-text-tertiary mt-0.5">Supported</div>
            </div>
            <div class="bg-surface-2/50 border border-border/50 rounded-xl p-4 text-center">
              <div class="text-lg font-bold text-text-primary">CSV</div>
              <div class="text-xs text-text-tertiary mt-0.5">Supported</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Right: Register Form -->
      <div class="flex-1 flex items-center justify-center px-6 py-12">
        <div class="w-full max-w-sm">

          <!-- Mobile logo -->
          <div class="lg:hidden text-center mb-10 reveal">
            <div class="text-4xl mb-2">&#9889;</div>
            <h1 class="text-2xl font-bold text-text-primary tracking-tight">Create your account</h1>
          </div>

          <!-- Desktop heading -->
          <div class="hidden lg:block mb-10 reveal">
            <h1 class="text-2xl font-bold text-text-primary tracking-tight mb-2">Create your account</h1>
            <p class="text-text-secondary text-sm">Start building chatbots in under a minute.</p>
          </div>

          <!-- Error -->
          @if (error()) {
            <div class="border-l-4 border-red-500 bg-red-500/10 text-red-400 text-sm
                        px-4 py-3 rounded-r-lg mb-6 animate-slide-up">
              {{ error() }}
            </div>
          }

          <form (ngSubmit)="onSubmit()" class="flex flex-col gap-5">
            <div class="reveal reveal-d1">
              <label class="block text-sm font-medium text-text-secondary mb-2">Display Name</label>
              <input type="text" [(ngModel)]="displayName" name="displayName" required
                     placeholder="Jane Doe"
                     class="w-full px-4 py-3 bg-surface-2 border border-border rounded-xl
                            text-text-primary text-sm placeholder:text-text-muted
                            focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20
                            transition-all duration-200" />
            </div>

            <div class="reveal reveal-d2">
              <label class="block text-sm font-medium text-text-secondary mb-2">Email</label>
              <input type="email" [(ngModel)]="email" name="email" required
                     placeholder="you&#64;company.com"
                     class="w-full px-4 py-3 bg-surface-2 border border-border rounded-xl
                            text-text-primary text-sm placeholder:text-text-muted
                            focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20
                            transition-all duration-200" />
            </div>

            <div class="reveal reveal-d3">
              <label class="block text-sm font-medium text-text-secondary mb-2">Password</label>
              <input type="password" [(ngModel)]="password" name="password" required minlength="8"
                     placeholder="Min 8 characters"
                     class="w-full px-4 py-3 bg-surface-2 border border-border rounded-xl
                            text-text-primary text-sm placeholder:text-text-muted
                            focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20
                            transition-all duration-200" />

              <!-- Password strength indicator -->
              @if (password.length > 0) {
                <div class="mt-3 flex items-center gap-3">
                  <div class="flex gap-1.5 flex-1">
                    <div class="h-1 flex-1 rounded-full transition-all duration-300"
                         [class]="strength().level >= 1 ? strength().barColor : 'bg-surface-3'"></div>
                    <div class="h-1 flex-1 rounded-full transition-all duration-300"
                         [class]="strength().level >= 2 ? strength().barColor : 'bg-surface-3'"></div>
                    <div class="h-1 flex-1 rounded-full transition-all duration-300"
                         [class]="strength().level >= 3 ? strength().barColor : 'bg-surface-3'"></div>
                  </div>
                  <span class="text-xs font-medium min-w-[40px] text-right"
                        [class]="strength().textColor">
                    {{ strength().label }}
                  </span>
                </div>
              }
            </div>

            <div class="reveal reveal-d4">
              <button type="submit" [disabled]="loading()"
                      class="w-full py-3 bg-accent hover:bg-accent-hover text-white font-semibold
                             rounded-xl cursor-pointer transition-all duration-200 border-none
                             shadow-lg shadow-accent/25 hover:shadow-xl hover:shadow-accent/30
                             hover:-translate-y-0.5 active:translate-y-0
                             disabled:opacity-50 disabled:cursor-not-allowed
                             disabled:hover:translate-y-0 disabled:hover:shadow-lg">
                {{ loading() ? 'Creating account...' : 'Create account' }}
              </button>
            </div>
          </form>

          <p class="text-center text-sm text-text-tertiary mt-8 reveal reveal-d5">
            Already have an account?
            <a routerLink="/login"
               class="text-accent hover:text-accent-hover font-semibold transition-colors ml-1">
              Sign in
            </a>
          </p>
        </div>
      </div>
    </div>
  `,
})
export class RegisterComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  displayName = '';
  email = '';
  password = '';
  loading = signal(false);
  error = signal('');

  strength = computed(() => {
    const p = this.password;
    if (p.length < 4) return { level: 0, label: '', barColor: 'bg-surface-3', textColor: 'text-text-muted' };
    if (p.length < 8) return { level: 1, label: 'Weak', barColor: 'bg-red-500', textColor: 'text-red-400' };

    const hasUpper = /[A-Z]/.test(p);
    const hasDigit = /\d/.test(p);
    const hasSpecial = /[^A-Za-z0-9]/.test(p);
    const complexity = [hasUpper, hasDigit, hasSpecial].filter(Boolean).length;

    if (complexity >= 2 && p.length >= 10)
      return { level: 3, label: 'Strong', barColor: 'bg-emerald-500', textColor: 'text-emerald-400' };
    if (complexity >= 1)
      return { level: 2, label: 'Fair', barColor: 'bg-amber-500', textColor: 'text-amber-400' };
    return { level: 1, label: 'Weak', barColor: 'bg-red-500', textColor: 'text-red-400' };
  });

  async onSubmit() {
    this.loading.set(true);
    this.error.set('');
    try {
      await this.auth.register(this.email, this.password, this.displayName);
      this.router.navigate(['/dashboard']);
    } catch (e: any) {
      const msg = e?.error?.error || 'Registration failed';
      this.error.set(msg);
      this.toast.error(msg);
    } finally {
      this.loading.set(false);
    }
  }
}
