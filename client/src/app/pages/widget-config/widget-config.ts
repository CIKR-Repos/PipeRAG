import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { NavbarComponent } from '../../shared/components/navbar/navbar';
import { ToastService } from '../../core/services/toast.service';

interface WidgetConfig {
  id: string;
  projectId: string;
  primaryColor: string;
  backgroundColor: string;
  textColor: string;
  position: string;
  avatarUrl: string | null;
  title: string;
  subtitle: string;
  placeholderText: string;
  allowedOrigins: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

@Component({
  selector: 'app-widget-config',
  standalone: true,
  imports: [FormsModule, NavbarComponent, RouterLink],
  template: `
    <app-navbar />
    <main class="max-w-6xl mx-auto px-6 py-8">

      <!-- Header -->
      <div class="flex items-center justify-between mb-8 animate-fade-in">
        <div>
          <h1 class="text-2xl font-bold text-text-primary tracking-tight">Chat Widget</h1>
          <p class="text-text-secondary text-sm mt-1">Configure and embed a chat widget on your website</p>
        </div>
        <a [routerLink]="['/projects', projectId, 'chat']"
           class="flex items-center gap-2 px-4 py-2 bg-surface-2 hover:bg-surface-3 text-text-secondary
                  hover:text-text-primary rounded-xl text-sm transition-all duration-200 no-underline
                  border border-border">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
          Back to Chat
        </a>
      </div>

      @if (loading()) {
        <!-- Skeleton Loading -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
          <div class="space-y-6">
            @for (i of [1,2,3,4]; track i) {
              <div class="bg-surface-2 border border-border rounded-2xl p-6 animate-pulse">
                <div class="skeleton h-5 w-24 mb-4 rounded"></div>
                <div class="skeleton h-10 w-full rounded-xl"></div>
              </div>
            }
          </div>
          <div class="space-y-6">
            <div class="bg-surface-2 border border-border rounded-2xl p-6 animate-pulse">
              <div class="skeleton h-5 w-20 mb-4 rounded"></div>
              <div class="skeleton h-[400px] w-full rounded-xl"></div>
            </div>
          </div>
        </div>
      } @else {
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">

          <!-- Configuration Panel -->
          <div class="space-y-6">

            <!-- Status -->
            <section class="bg-surface-2 border border-border rounded-2xl p-6
                            hover:border-border-subtle transition-all duration-300">
              <div class="flex items-center justify-between">
                <div>
                  <h2 class="text-sm font-semibold text-text-primary mb-1">Widget Status</h2>
                  <p class="text-xs text-text-tertiary">
                    {{ config.isActive ? 'Widget is live on allowed origins' : 'Widget is currently disabled' }}
                  </p>
                </div>
                <label class="relative inline-flex items-center cursor-pointer">
                  <input type="checkbox" [(ngModel)]="config.isActive" class="sr-only peer">
                  <div class="w-11 h-6 bg-surface-3 peer-focus:ring-2 peer-focus:ring-accent/20
                              rounded-full peer peer-checked:after:translate-x-full
                              after:content-[''] after:absolute after:top-[2px] after:left-[2px]
                              after:bg-text-tertiary after:rounded-full after:h-5 after:w-5
                              after:transition-all peer-checked:bg-accent peer-checked:after:bg-white">
                  </div>
                </label>
              </div>
            </section>

            <!-- Display -->
            <section class="bg-surface-2 border border-border rounded-2xl p-6
                            hover:border-border-subtle transition-all duration-300">
              <h2 class="text-sm font-semibold text-text-primary mb-4 flex items-center gap-2">
                <svg class="w-4 h-4 text-text-tertiary" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round"
                        d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z" />
                </svg>
                Display
              </h2>
              <div class="space-y-4">
                <div>
                  <label class="block text-xs font-medium text-text-tertiary mb-1.5">Title</label>
                  <input type="text" [(ngModel)]="config.title"
                    class="w-full px-4 py-2.5 bg-surface-1 border border-border rounded-xl text-sm
                           text-text-primary placeholder:text-text-muted
                           focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20
                           transition-all duration-200">
                </div>
                <div>
                  <label class="block text-xs font-medium text-text-tertiary mb-1.5">Subtitle</label>
                  <input type="text" [(ngModel)]="config.subtitle"
                    class="w-full px-4 py-2.5 bg-surface-1 border border-border rounded-xl text-sm
                           text-text-primary placeholder:text-text-muted
                           focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20
                           transition-all duration-200">
                </div>
                <div>
                  <label class="block text-xs font-medium text-text-tertiary mb-1.5">Placeholder Text</label>
                  <input type="text" [(ngModel)]="config.placeholderText"
                    class="w-full px-4 py-2.5 bg-surface-1 border border-border rounded-xl text-sm
                           text-text-primary placeholder:text-text-muted
                           focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20
                           transition-all duration-200">
                </div>
                <div>
                  <label class="block text-xs font-medium text-text-tertiary mb-1.5">Avatar URL</label>
                  <input type="url" [(ngModel)]="config.avatarUrl"
                    class="w-full px-4 py-2.5 bg-surface-1 border border-border rounded-xl text-sm
                           text-text-primary placeholder:text-text-muted
                           focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20
                           transition-all duration-200"
                    placeholder="https://example.com/avatar.png">
                </div>
              </div>
            </section>

            <!-- Theme -->
            <section class="bg-surface-2 border border-border rounded-2xl p-6
                            hover:border-border-subtle transition-all duration-300">
              <h2 class="text-sm font-semibold text-text-primary mb-4 flex items-center gap-2">
                <svg class="w-4 h-4 text-text-tertiary" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round"
                        d="M7 21a4 4 0 01-4-4V5a2 2 0 012-2h4a2 2 0 012 2v12a4 4 0 01-4 4zm0 0h12a2 2 0 002-2v-4a2 2 0 00-2-2h-2.343M11 7.343l1.657-1.657a2 2 0 012.828 0l2.829 2.829a2 2 0 010 2.828l-8.486 8.485M7 17h.01" />
                </svg>
                Theme Colors
              </h2>
              <div class="grid grid-cols-3 gap-4">
                <div>
                  <label class="block text-xs font-medium text-text-tertiary mb-2">Primary</label>
                  <div class="flex items-center gap-2 bg-surface-1 border border-border rounded-xl px-2 py-1.5">
                    <input type="color" [(ngModel)]="config.primaryColor"
                      class="w-8 h-8 rounded-lg cursor-pointer border-0 bg-transparent shrink-0
                             [&::-webkit-color-swatch-wrapper]:p-0 [&::-webkit-color-swatch]:rounded-md [&::-webkit-color-swatch]:border-0">
                    <span class="text-xs text-text-tertiary font-mono truncate">{{ config.primaryColor }}</span>
                  </div>
                </div>
                <div>
                  <label class="block text-xs font-medium text-text-tertiary mb-2">Background</label>
                  <div class="flex items-center gap-2 bg-surface-1 border border-border rounded-xl px-2 py-1.5">
                    <input type="color" [(ngModel)]="config.backgroundColor"
                      class="w-8 h-8 rounded-lg cursor-pointer border-0 bg-transparent shrink-0
                             [&::-webkit-color-swatch-wrapper]:p-0 [&::-webkit-color-swatch]:rounded-md [&::-webkit-color-swatch]:border-0">
                    <span class="text-xs text-text-tertiary font-mono truncate">{{ config.backgroundColor }}</span>
                  </div>
                </div>
                <div>
                  <label class="block text-xs font-medium text-text-tertiary mb-2">Text</label>
                  <div class="flex items-center gap-2 bg-surface-1 border border-border rounded-xl px-2 py-1.5">
                    <input type="color" [(ngModel)]="config.textColor"
                      class="w-8 h-8 rounded-lg cursor-pointer border-0 bg-transparent shrink-0
                             [&::-webkit-color-swatch-wrapper]:p-0 [&::-webkit-color-swatch]:rounded-md [&::-webkit-color-swatch]:border-0">
                    <span class="text-xs text-text-tertiary font-mono truncate">{{ config.textColor }}</span>
                  </div>
                </div>
              </div>
            </section>

            <!-- Position -->
            <section class="bg-surface-2 border border-border rounded-2xl p-6
                            hover:border-border-subtle transition-all duration-300">
              <h2 class="text-sm font-semibold text-text-primary mb-4 flex items-center gap-2">
                <svg class="w-4 h-4 text-text-tertiary" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round"
                        d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
                </svg>
                Widget Position
              </h2>
              <div class="flex gap-3">
                <button (click)="config.position = 'bottom-right'"
                  class="flex-1 flex items-center justify-center gap-2 px-4 py-3 rounded-xl text-sm
                         font-medium transition-all duration-200 border cursor-pointer"
                  [class]="config.position === 'bottom-right'
                    ? 'bg-accent/10 border-accent/50 text-accent'
                    : 'bg-surface-1 border-border text-text-tertiary hover:text-text-secondary hover:border-border-subtle'">
                  <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <rect x="3" y="3" width="18" height="18" rx="3" stroke-dasharray="3 3" />
                    <rect x="13" y="13" width="7" height="7" rx="2" fill="currentColor" opacity="0.3" stroke="currentColor" />
                  </svg>
                  Bottom Right
                </button>
                <button (click)="config.position = 'bottom-left'"
                  class="flex-1 flex items-center justify-center gap-2 px-4 py-3 rounded-xl text-sm
                         font-medium transition-all duration-200 border cursor-pointer"
                  [class]="config.position === 'bottom-left'
                    ? 'bg-accent/10 border-accent/50 text-accent'
                    : 'bg-surface-1 border-border text-text-tertiary hover:text-text-secondary hover:border-border-subtle'">
                  <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                    <rect x="3" y="3" width="18" height="18" rx="3" stroke-dasharray="3 3" />
                    <rect x="4" y="13" width="7" height="7" rx="2" fill="currentColor" opacity="0.3" stroke="currentColor" />
                  </svg>
                  Bottom Left
                </button>
              </div>
            </section>

            <!-- Security -->
            <section class="bg-surface-2 border border-border rounded-2xl p-6
                            hover:border-border-subtle transition-all duration-300">
              <h2 class="text-sm font-semibold text-text-primary mb-4 flex items-center gap-2">
                <svg class="w-4 h-4 text-text-tertiary" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round"
                        d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
                Allowed Origins
              </h2>
              <input type="text" [(ngModel)]="config.allowedOrigins"
                class="w-full px-4 py-2.5 bg-surface-1 border border-border rounded-xl text-sm
                       text-text-primary placeholder:text-text-muted
                       focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20
                       transition-all duration-200"
                placeholder="* or https://example.com,https://other.com">
              <p class="text-xs text-text-muted mt-2">Comma-separated origins, or * for all domains</p>
            </section>

            <!-- Save -->
            <div class="flex items-center gap-4">
              <button (click)="save()"
                [disabled]="saving()"
                class="px-6 py-2.5 bg-accent hover:bg-accent-hover text-white rounded-xl
                       font-semibold text-sm cursor-pointer transition-all duration-200 border-none
                       shadow-lg shadow-accent/25 hover:shadow-xl hover:shadow-accent/30
                       hover:-translate-y-0.5 active:translate-y-0
                       disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:translate-y-0
                       disabled:hover:shadow-lg">
                {{ saving() ? 'Saving...' : 'Save Configuration' }}
              </button>
            </div>
          </div>

          <!-- Preview & Embed Code -->
          <div class="space-y-6">

            <!-- Live Preview -->
            <section class="bg-surface-2 border border-border rounded-2xl p-6
                            hover:border-border-subtle transition-all duration-300">
              <h2 class="text-sm font-semibold text-text-primary mb-4 flex items-center gap-2">
                <svg class="w-4 h-4 text-text-tertiary" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                  <path stroke-linecap="round" stroke-linejoin="round"
                        d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
                Live Preview
              </h2>

              <!-- Browser Chrome -->
              <div class="rounded-xl overflow-hidden border border-border shadow-lg shadow-black/20">
                <!-- Title bar -->
                <div class="bg-surface-3 px-4 py-2.5 flex items-center gap-3 border-b border-border">
                  <div class="flex gap-1.5">
                    <span class="w-3 h-3 rounded-full bg-red-500/60"></span>
                    <span class="w-3 h-3 rounded-full bg-amber-500/60"></span>
                    <span class="w-3 h-3 rounded-full bg-emerald-500/60"></span>
                  </div>
                  <div class="flex-1 bg-surface-1 rounded-md px-3 py-1 text-xs text-text-muted text-center truncate">
                    yourwebsite.com
                  </div>
                </div>

                <!-- Widget Preview Content -->
                <div class="relative bg-surface-1" style="height: 380px">
                  <!-- Placeholder site content -->
                  <div class="p-6 space-y-3 opacity-30">
                    <div class="h-4 w-32 bg-surface-3 rounded"></div>
                    <div class="h-3 w-full bg-surface-3 rounded"></div>
                    <div class="h-3 w-3/4 bg-surface-3 rounded"></div>
                    <div class="h-3 w-5/6 bg-surface-3 rounded"></div>
                    <div class="h-20 w-full bg-surface-3 rounded-lg mt-4"></div>
                  </div>

                  <!-- Widget popup positioned in corner -->
                  <div class="absolute bottom-3 w-64 rounded-xl overflow-hidden shadow-2xl shadow-black/40
                              border border-white/10"
                       [class]="config.position === 'bottom-right' ? 'right-3' : 'left-3'">
                    <!-- Widget header -->
                    <div [style.background]="config.primaryColor" class="p-3 text-white">
                      <div class="flex items-center gap-2.5">
                        @if (config.avatarUrl) {
                          <img [src]="config.avatarUrl" class="w-7 h-7 rounded-full object-cover"
                               (error)="config.avatarUrl = null">
                        } @else {
                          <div class="w-7 h-7 rounded-full bg-white/20 flex items-center justify-center text-xs">
                            &#128172;
                          </div>
                        }
                        <div class="min-w-0">
                          <div class="font-semibold text-xs truncate">{{ config.title }}</div>
                          <div class="text-[10px] opacity-80 truncate">{{ config.subtitle }}</div>
                        </div>
                      </div>
                    </div>

                    <!-- Widget messages -->
                    <div [style.background]="config.backgroundColor" class="p-3" style="min-height:140px">
                      <div class="space-y-2">
                        <div class="flex justify-end">
                          <div [style.background]="config.primaryColor"
                               class="px-2.5 py-1.5 rounded-xl rounded-br-sm text-white text-xs max-w-[85%]">
                            How do I get started?
                          </div>
                        </div>
                        <div class="flex justify-start">
                          <div class="px-2.5 py-1.5 rounded-xl rounded-bl-sm text-xs max-w-[85%]"
                               [style.color]="config.textColor"
                               style="background:rgba(255,255,255,0.08)">
                            Welcome! Upload your documents to start querying.
                          </div>
                        </div>
                      </div>
                    </div>

                    <!-- Widget input -->
                    <div [style.background]="config.backgroundColor"
                         class="px-3 pb-3 flex gap-1.5">
                      <input type="text" disabled [placeholder]="config.placeholderText"
                        class="flex-1 px-2.5 py-1.5 rounded-lg text-xs border-0
                               bg-white/10 placeholder:opacity-50"
                        [style.color]="config.textColor">
                      <button [style.background]="config.primaryColor"
                        class="px-3 py-1.5 rounded-lg text-white text-xs border-0 shrink-0">Send</button>
                    </div>
                  </div>
                </div>
              </div>
            </section>

            <!-- Embed Code -->
            <section class="bg-surface-2 border border-border rounded-2xl p-6
                            hover:border-border-subtle transition-all duration-300">
              <h2 class="text-sm font-semibold text-text-primary mb-2 flex items-center gap-2">
                <svg class="w-4 h-4 text-text-tertiary" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
                </svg>
                Embed Code
              </h2>
              <p class="text-xs text-text-muted mb-3">Add this snippet to your website's HTML:</p>
              <div class="relative group">
                <pre class="bg-surface-0 border border-border rounded-xl p-4 text-xs text-emerald-400
                            overflow-x-auto font-mono scrollbar-thin leading-relaxed"><code>{{ embedCode }}</code></pre>
                <button (click)="copyEmbed()"
                  class="absolute top-3 right-3 px-3 py-1.5 rounded-lg text-xs font-medium
                         transition-all duration-200 border cursor-pointer
                         opacity-0 group-hover:opacity-100"
                  [class]="copied()
                    ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400'
                    : 'bg-surface-2 border-border text-text-tertiary hover:text-text-primary hover:border-border-subtle'">
                  {{ copied() ? 'Copied!' : 'Copy' }}
                </button>
              </div>
              <p class="text-xs text-text-muted mt-3">
                Replace <code class="text-accent font-mono text-[11px] bg-accent/10 px-1.5 py-0.5 rounded">YOUR_API_KEY</code>
                with a valid project API key.
              </p>
            </section>
          </div>

        </div>
      }
    </main>
  `,
})
export class WidgetConfigComponent implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private toast = inject(ToastService);

  projectId = '';
  loading = signal(true);
  saving = signal(false);
  saved = signal(false);
  copied = signal(false);

  config = {
    primaryColor: '#6366f1',
    backgroundColor: '#1e1e2e',
    textColor: '#ffffff',
    position: 'bottom-right',
    avatarUrl: null as string | null,
    title: 'Chat with us',
    subtitle: 'Ask anything about our docs',
    placeholderText: 'Type a message...',
    allowedOrigins: '*',
    isActive: true,
  };

  get embedCode(): string {
    const base = window.location.origin;
    return `<script src="${base}/api/widget/embed.js"\n  data-project-id="${this.projectId}"\n  data-api-key="YOUR_API_KEY"><\/script>`;
  }

  ngOnInit() {
    this.projectId = this.route.snapshot.paramMap.get('projectId') ?? '';
    this.loadConfig();
  }

  loadConfig() {
    this.loading.set(true);
    this.http.get<WidgetConfig>(`/api/projects/${this.projectId}/widget`).subscribe({
      next: (cfg) => {
        this.config = {
          primaryColor: cfg.primaryColor,
          backgroundColor: cfg.backgroundColor,
          textColor: cfg.textColor,
          position: cfg.position,
          avatarUrl: cfg.avatarUrl,
          title: cfg.title,
          subtitle: cfg.subtitle,
          placeholderText: cfg.placeholderText,
          allowedOrigins: cfg.allowedOrigins,
          isActive: cfg.isActive,
        };
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  save() {
    this.saving.set(true);
    this.saved.set(false);
    this.http.put(`/api/projects/${this.projectId}/widget`, this.config).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
        this.toast.success('Widget configuration saved');
        setTimeout(() => this.saved.set(false), 3000);
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Failed to save configuration');
      },
    });
  }

  copyEmbed() {
    navigator.clipboard.writeText(this.embedCode).then(() => {
      this.copied.set(true);
      this.toast.success('Embed code copied to clipboard');
      setTimeout(() => this.copied.set(false), 2000);
    });
  }
}
