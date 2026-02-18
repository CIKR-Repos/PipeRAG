import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { NavbarComponent } from '../../shared/components/navbar/navbar';

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
  imports: [CommonModule, FormsModule, NavbarComponent],
  template: `
    <app-navbar />
    <main class="max-w-5xl mx-auto px-6 py-8">
      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-3xl font-bold text-white">Chat Widget</h1>
          <p class="text-white/60 mt-1">Configure and embed a chat widget on your website</p>
        </div>
        <a [href]="'/projects/' + projectId + '/chat'"
           class="px-4 py-2 bg-white/10 hover:bg-white/20 text-white rounded-lg text-sm transition">
          ← Back to Chat
        </a>
      </div>

      @if (loading()) {
        <div class="text-center py-20 text-white/60">Loading...</div>
      } @else {
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
          <!-- Configuration Panel -->
          <div class="space-y-6">
            <!-- Status -->
            <section class="bg-dark-card border border-white/10 rounded-xl p-6">
              <h2 class="text-lg font-semibold text-white mb-4">Status</h2>
              <label class="flex items-center gap-3 cursor-pointer">
                <input type="checkbox" [(ngModel)]="config.isActive"
                  class="w-5 h-5 rounded accent-indigo-500">
                <span class="text-white">Widget Active</span>
              </label>
            </section>

            <!-- Display -->
            <section class="bg-dark-card border border-white/10 rounded-xl p-6">
              <h2 class="text-lg font-semibold text-white mb-4">Display</h2>
              <div class="space-y-4">
                <div>
                  <label class="block text-sm text-white/70 mb-1">Title</label>
                  <input type="text" [(ngModel)]="config.title"
                    class="w-full px-4 py-2 bg-white/5 border border-white/10 rounded-lg text-white focus:border-indigo-500 focus:outline-none">
                </div>
                <div>
                  <label class="block text-sm text-white/70 mb-1">Subtitle</label>
                  <input type="text" [(ngModel)]="config.subtitle"
                    class="w-full px-4 py-2 bg-white/5 border border-white/10 rounded-lg text-white focus:border-indigo-500 focus:outline-none">
                </div>
                <div>
                  <label class="block text-sm text-white/70 mb-1">Placeholder Text</label>
                  <input type="text" [(ngModel)]="config.placeholderText"
                    class="w-full px-4 py-2 bg-white/5 border border-white/10 rounded-lg text-white focus:border-indigo-500 focus:outline-none">
                </div>
                <div>
                  <label class="block text-sm text-white/70 mb-1">Avatar URL</label>
                  <input type="url" [(ngModel)]="config.avatarUrl"
                    class="w-full px-4 py-2 bg-white/5 border border-white/10 rounded-lg text-white focus:border-indigo-500 focus:outline-none"
                    placeholder="https://example.com/avatar.png">
                </div>
              </div>
            </section>

            <!-- Theme -->
            <section class="bg-dark-card border border-white/10 rounded-xl p-6">
              <h2 class="text-lg font-semibold text-white mb-4">Theme</h2>
              <div class="grid grid-cols-3 gap-4">
                <div>
                  <label class="block text-sm text-white/70 mb-1">Primary</label>
                  <div class="flex items-center gap-2">
                    <input type="color" [(ngModel)]="config.primaryColor"
                      class="w-10 h-10 rounded cursor-pointer border-0 bg-transparent">
                    <span class="text-sm text-white/50">{{ config.primaryColor }}</span>
                  </div>
                </div>
                <div>
                  <label class="block text-sm text-white/70 mb-1">Background</label>
                  <div class="flex items-center gap-2">
                    <input type="color" [(ngModel)]="config.backgroundColor"
                      class="w-10 h-10 rounded cursor-pointer border-0 bg-transparent">
                    <span class="text-sm text-white/50">{{ config.backgroundColor }}</span>
                  </div>
                </div>
                <div>
                  <label class="block text-sm text-white/70 mb-1">Text</label>
                  <div class="flex items-center gap-2">
                    <input type="color" [(ngModel)]="config.textColor"
                      class="w-10 h-10 rounded cursor-pointer border-0 bg-transparent">
                    <span class="text-sm text-white/50">{{ config.textColor }}</span>
                  </div>
                </div>
              </div>
            </section>

            <!-- Position -->
            <section class="bg-dark-card border border-white/10 rounded-xl p-6">
              <h2 class="text-lg font-semibold text-white mb-4">Position</h2>
              <div class="flex gap-4">
                <button (click)="config.position = 'bottom-right'"
                  [class]="'px-4 py-2 rounded-lg text-sm font-medium transition ' + (config.position === 'bottom-right' ? 'bg-indigo-500 text-white' : 'bg-white/10 text-white/70 hover:bg-white/20')">
                  Bottom Right
                </button>
                <button (click)="config.position = 'bottom-left'"
                  [class]="'px-4 py-2 rounded-lg text-sm font-medium transition ' + (config.position === 'bottom-left' ? 'bg-indigo-500 text-white' : 'bg-white/10 text-white/70 hover:bg-white/20')">
                  Bottom Left
                </button>
              </div>
            </section>

            <!-- Security -->
            <section class="bg-dark-card border border-white/10 rounded-xl p-6">
              <h2 class="text-lg font-semibold text-white mb-4">Allowed Origins</h2>
              <input type="text" [(ngModel)]="config.allowedOrigins"
                class="w-full px-4 py-2 bg-white/5 border border-white/10 rounded-lg text-white focus:border-indigo-500 focus:outline-none"
                placeholder="* or https://example.com,https://other.com">
              <p class="text-xs text-white/40 mt-2">Comma-separated origins, or * for all</p>
            </section>

            <!-- Save -->
            <div class="flex gap-4">
              <button (click)="save()"
                [disabled]="saving()"
                class="px-6 py-3 bg-indigo-500 hover:bg-indigo-600 disabled:opacity-50 text-white rounded-lg font-medium transition">
                {{ saving() ? 'Saving...' : 'Save Configuration' }}
              </button>
              @if (saved()) {
                <span class="text-green-400 self-center text-sm">✓ Saved</span>
              }
            </div>
          </div>

          <!-- Preview & Embed Code -->
          <div class="space-y-6">
            <!-- Live Preview -->
            <section class="bg-dark-card border border-white/10 rounded-xl p-6">
              <h2 class="text-lg font-semibold text-white mb-4">Preview</h2>
              <div class="rounded-xl overflow-hidden border border-white/10"
                   [style.height.px]="400">
                <div [style.background]="config.primaryColor" class="p-4 text-white">
                  <div class="flex items-center gap-3">
                    @if (config.avatarUrl) {
                      <img [src]="config.avatarUrl" class="w-8 h-8 rounded-full object-cover"
                           (error)="config.avatarUrl = null">
                    }
                    <div>
                      <div class="font-semibold text-sm">{{ config.title }}</div>
                      <div class="text-xs opacity-80">{{ config.subtitle }}</div>
                    </div>
                  </div>
                </div>
                <div [style.background]="config.backgroundColor" class="p-4 flex-1" style="min-height:260px">
                  <div class="space-y-3">
                    <div class="flex justify-end">
                      <div [style.background]="config.primaryColor"
                           class="px-3 py-2 rounded-xl rounded-br-sm text-white text-sm max-w-[80%]">
                        How do I get started?
                      </div>
                    </div>
                    <div class="flex justify-start">
                      <div class="px-3 py-2 rounded-xl rounded-bl-sm text-sm max-w-[80%]"
                           [style.color]="config.textColor"
                           style="background:rgba(255,255,255,0.1)">
                        Welcome! Upload your documents and create a pipeline to start querying.
                      </div>
                    </div>
                  </div>
                </div>
                <div [style.background]="config.backgroundColor"
                     class="p-3 border-t border-white/10 flex gap-2">
                  <input type="text" disabled [placeholder]="config.placeholderText"
                    class="flex-1 px-3 py-2 rounded-full bg-white/5 border border-white/10 text-sm"
                    [style.color]="config.textColor">
                  <button [style.background]="config.primaryColor"
                    class="px-4 py-2 rounded-full text-white text-sm">Send</button>
                </div>
              </div>
            </section>

            <!-- Embed Code -->
            <section class="bg-dark-card border border-white/10 rounded-xl p-6">
              <h2 class="text-lg font-semibold text-white mb-4">Embed Code</h2>
              <p class="text-sm text-white/60 mb-3">Add this snippet to your website's HTML:</p>
              <div class="relative">
                <pre class="bg-black/50 rounded-lg p-4 text-sm text-green-400 overflow-x-auto"><code>{{ embedCode }}</code></pre>
                <button (click)="copyEmbed()"
                  class="absolute top-2 right-2 px-3 py-1 bg-white/10 hover:bg-white/20 text-white text-xs rounded-lg transition">
                  {{ copied() ? '✓ Copied' : 'Copy' }}
                </button>
              </div>
              <p class="text-xs text-white/40 mt-3">
                Replace <code class="text-white/60">YOUR_API_KEY</code> with a valid project API key.
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
      error: () => this.loading.set(false), // 404 = not configured yet, use defaults
    });
  }

  save() {
    this.saving.set(true);
    this.saved.set(false);
    this.http.put(`/api/projects/${this.projectId}/widget`, this.config).subscribe({
      next: () => { this.saving.set(false); this.saved.set(true); setTimeout(() => this.saved.set(false), 3000); },
      error: () => this.saving.set(false),
    });
  }

  copyEmbed() {
    navigator.clipboard.writeText(this.embedCode).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }
}
