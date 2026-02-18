import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
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
  selector: 'app-widget-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent, RouterLink],
  template: `
    <app-navbar />
    <div class="min-h-screen bg-gray-950 text-white">
      <div class="max-w-5xl mx-auto px-6 py-8">
        <!-- Header -->
        <div class="flex items-center gap-4 mb-8">
          <a [routerLink]="['/dashboard']" class="text-gray-400 hover:text-white transition">
            ← Back
          </a>
          <h1 class="text-2xl font-bold">Chat Widget Settings</h1>
        </div>

        @if (loading()) {
          <div class="flex items-center justify-center py-20">
            <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-500"></div>
          </div>
        } @else {
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
            <!-- Settings Panel -->
            <div class="space-y-6">
              <!-- Enable/Disable -->
              <div class="bg-gray-900 rounded-xl p-6 border border-gray-800">
                <div class="flex items-center justify-between">
                  <div>
                    <h3 class="font-semibold text-lg">Widget Status</h3>
                    <p class="text-sm text-gray-400 mt-1">Enable or disable the chat widget</p>
                  </div>
                  <button
                    (click)="toggleActive()"
                    [class]="config().isActive
                      ? 'bg-indigo-600 relative inline-flex h-6 w-11 items-center rounded-full transition'
                      : 'bg-gray-700 relative inline-flex h-6 w-11 items-center rounded-full transition'">
                    <span [class]="config().isActive
                      ? 'translate-x-6 inline-block h-4 w-4 rounded-full bg-white transition'
                      : 'translate-x-1 inline-block h-4 w-4 rounded-full bg-white transition'"></span>
                  </button>
                </div>
              </div>

              <!-- Theme Colors -->
              <div class="bg-gray-900 rounded-xl p-6 border border-gray-800">
                <h3 class="font-semibold text-lg mb-4">Theme</h3>
                <div class="space-y-4">
                  <div class="flex items-center gap-4">
                    <label class="w-36 text-sm text-gray-300">Primary Color</label>
                    <input type="color" [(ngModel)]="config().primaryColor"
                      class="h-10 w-14 rounded border border-gray-700 bg-gray-800 cursor-pointer" />
                    <input type="text" [(ngModel)]="config().primaryColor"
                      class="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm font-mono" />
                  </div>
                  <div class="flex items-center gap-4">
                    <label class="w-36 text-sm text-gray-300">Background</label>
                    <input type="color" [(ngModel)]="config().backgroundColor"
                      class="h-10 w-14 rounded border border-gray-700 bg-gray-800 cursor-pointer" />
                    <input type="text" [(ngModel)]="config().backgroundColor"
                      class="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm font-mono" />
                  </div>
                  <div class="flex items-center gap-4">
                    <label class="w-36 text-sm text-gray-300">Text Color</label>
                    <input type="color" [(ngModel)]="config().textColor"
                      class="h-10 w-14 rounded border border-gray-700 bg-gray-800 cursor-pointer" />
                    <input type="text" [(ngModel)]="config().textColor"
                      class="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm font-mono" />
                  </div>
                </div>
              </div>

              <!-- Display Settings -->
              <div class="bg-gray-900 rounded-xl p-6 border border-gray-800">
                <h3 class="font-semibold text-lg mb-4">Display</h3>
                <div class="space-y-4">
                  <div>
                    <label class="block text-sm text-gray-300 mb-1">Title</label>
                    <input type="text" [(ngModel)]="config().title"
                      class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm" />
                  </div>
                  <div>
                    <label class="block text-sm text-gray-300 mb-1">Subtitle</label>
                    <input type="text" [(ngModel)]="config().subtitle"
                      class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm" />
                  </div>
                  <div>
                    <label class="block text-sm text-gray-300 mb-1">Placeholder Text</label>
                    <input type="text" [(ngModel)]="config().placeholderText"
                      class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm" />
                  </div>
                  <div>
                    <label class="block text-sm text-gray-300 mb-1">Position</label>
                    <select [(ngModel)]="config().position"
                      class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
                      <option value="bottom-right">Bottom Right</option>
                      <option value="bottom-left">Bottom Left</option>
                    </select>
                  </div>
                  <div>
                    <label class="block text-sm text-gray-300 mb-1">Avatar URL (optional)</label>
                    <input type="text" [(ngModel)]="config().avatarUrl"
                      placeholder="https://example.com/avatar.png"
                      class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm" />
                  </div>
                </div>
              </div>

              <!-- Security -->
              <div class="bg-gray-900 rounded-xl p-6 border border-gray-800">
                <h3 class="font-semibold text-lg mb-4">Security</h3>
                <div>
                  <label class="block text-sm text-gray-300 mb-1">Allowed Origins</label>
                  <input type="text" [(ngModel)]="config().allowedOrigins"
                    placeholder="* or https://example.com, https://app.example.com"
                    class="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm" />
                  <p class="text-xs text-gray-500 mt-1">Use * for all origins, or comma-separated URLs</p>
                </div>
              </div>

              <!-- Save -->
              <div class="flex gap-3">
                <button (click)="save()"
                  [disabled]="saving()"
                  class="bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white font-semibold px-6 py-2.5 rounded-lg transition">
                  {{ saving() ? 'Saving...' : 'Save Changes' }}
                </button>
                <button (click)="deleteConfig()"
                  [disabled]="saving()"
                  class="bg-red-600/20 hover:bg-red-600/30 text-red-400 font-semibold px-6 py-2.5 rounded-lg transition border border-red-600/30">
                  Delete Widget
                </button>
              </div>

              @if (saveMessage()) {
                <p class="text-sm" [class]="saveError() ? 'text-red-400' : 'text-green-400'">
                  {{ saveMessage() }}
                </p>
              }
            </div>

            <!-- Preview & Embed Code -->
            <div class="space-y-6">
              <!-- Live Preview -->
              <div class="bg-gray-900 rounded-xl border border-gray-800 overflow-hidden">
                <div class="px-6 py-4 border-b border-gray-800">
                  <h3 class="font-semibold">Live Preview</h3>
                </div>
                <div class="p-6 flex justify-center">
                  <div class="w-80 rounded-xl overflow-hidden shadow-2xl"
                    [style.background]="config().backgroundColor">
                    <!-- Preview Header -->
                    <div class="p-4 flex items-center gap-3" [style.background]="config().primaryColor">
                      <div class="w-8 h-8 rounded-full bg-white/20 flex items-center justify-center">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                          <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
                        </svg>
                      </div>
                      <div>
                        <div class="text-sm font-semibold text-white">{{ config().title }}</div>
                        <div class="text-xs text-white/70">{{ config().subtitle }}</div>
                      </div>
                    </div>
                    <!-- Preview Messages -->
                    <div class="p-4 space-y-3 min-h-[200px]" [style.color]="config().textColor">
                      <div class="p-2.5 rounded-lg text-sm" style="background:rgba(255,255,255,0.1);">
                        Hi! How can I help you today?
                      </div>
                      <div class="p-2.5 rounded-lg text-sm ml-auto max-w-[80%] text-white"
                        [style.background]="config().primaryColor">
                        What is PipeRAG?
                      </div>
                      <div class="p-2.5 rounded-lg text-sm" style="background:rgba(255,255,255,0.1);">
                        PipeRAG is a no-code RAG pipeline builder...
                      </div>
                    </div>
                    <!-- Preview Input -->
                    <div class="p-3 border-t" style="border-color:rgba(255,255,255,0.1);">
                      <div class="flex gap-2">
                        <div class="flex-1 rounded-lg px-3 py-2 text-sm" style="background:rgba(255,255,255,0.1);"
                          [style.color]="config().textColor + '66'">
                          {{ config().placeholderText }}
                        </div>
                        <div class="px-3 py-2 rounded-lg text-white text-sm font-semibold"
                          [style.background]="config().primaryColor">Send</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Embed Code -->
              <div class="bg-gray-900 rounded-xl border border-gray-800">
                <div class="px-6 py-4 border-b border-gray-800 flex items-center justify-between">
                  <h3 class="font-semibold">Embed Code</h3>
                  <button (click)="copyEmbed()" class="text-sm text-indigo-400 hover:text-indigo-300 transition">
                    {{ copied() ? '✓ Copied!' : 'Copy' }}
                  </button>
                </div>
                <div class="p-4">
                  <pre class="bg-gray-950 rounded-lg p-4 text-sm text-gray-300 overflow-x-auto whitespace-pre-wrap font-mono leading-relaxed">{{ embedCode() }}</pre>
                  <p class="text-xs text-gray-500 mt-3">
                    Add this code before the closing <code class="text-gray-400">&lt;/body&gt;</code> tag on your website.
                  </p>
                </div>
              </div>
            </div>
          </div>
        }
      </div>
    </div>
  `,
})
export class WidgetSettingsComponent implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);

  projectId = signal('');
  loading = signal(true);
  saving = signal(false);
  saveMessage = signal('');
  saveError = signal(false);
  copied = signal(false);

  config = signal<WidgetConfig>({
    id: '',
    projectId: '',
    primaryColor: '#6366f1',
    backgroundColor: '#1e1e2e',
    textColor: '#ffffff',
    position: 'bottom-right',
    avatarUrl: null,
    title: 'Chat with us',
    subtitle: 'Ask anything about our docs',
    placeholderText: 'Type a message...',
    allowedOrigins: '*',
    isActive: true,
    createdAt: '',
    updatedAt: null,
  });

  embedCode = computed(() => {
    const pid = this.projectId();
    return `<!-- PipeRAG Chat Widget -->
<script>
  window.PipeRAGWidget = {
    projectId: '${pid}',
    apiKey: 'YOUR_API_KEY_HERE'
  };
</script>
<script src="${window.location.origin}/api/widget/embed.js" defer></script>`;
  });

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('projectId') ?? '';
    this.projectId.set(id);
    this.loadConfig();
  }

  loadConfig() {
    this.loading.set(true);
    this.http.get<WidgetConfig>(`/api/projects/${this.projectId()}/widget`).subscribe({
      next: (data) => {
        this.config.set(data);
        this.loading.set(false);
      },
      error: () => {
        // No config yet, use defaults
        this.loading.set(false);
      },
    });
  }

  toggleActive() {
    const c = this.config();
    this.config.set({ ...c, isActive: !c.isActive });
  }

  save() {
    this.saving.set(true);
    this.saveMessage.set('');
    const c = this.config();
    this.http.put<WidgetConfig>(`/api/projects/${this.projectId()}/widget`, {
      primaryColor: c.primaryColor,
      backgroundColor: c.backgroundColor,
      textColor: c.textColor,
      position: c.position,
      avatarUrl: c.avatarUrl,
      title: c.title,
      subtitle: c.subtitle,
      placeholderText: c.placeholderText,
      allowedOrigins: c.allowedOrigins,
      isActive: c.isActive,
    }).subscribe({
      next: (data) => {
        this.config.set(data);
        this.saving.set(false);
        this.saveMessage.set('Widget settings saved!');
        this.saveError.set(false);
      },
      error: () => {
        this.saving.set(false);
        this.saveMessage.set('Failed to save settings.');
        this.saveError.set(true);
      },
    });
  }

  deleteConfig() {
    if (!confirm('Delete widget configuration?')) return;
    this.saving.set(true);
    this.http.delete(`/api/projects/${this.projectId()}/widget`).subscribe({
      next: () => {
        this.saving.set(false);
        this.saveMessage.set('Widget configuration deleted.');
        this.saveError.set(false);
        this.config.set({
          id: '', projectId: this.projectId(), primaryColor: '#6366f1', backgroundColor: '#1e1e2e',
          textColor: '#ffffff', position: 'bottom-right', avatarUrl: null, title: 'Chat with us',
          subtitle: 'Ask anything about our docs', placeholderText: 'Type a message...',
          allowedOrigins: '*', isActive: true, createdAt: '', updatedAt: null,
        });
      },
      error: () => { this.saving.set(false); this.saveMessage.set('Failed to delete.'); this.saveError.set(true); },
    });
  }

  copyEmbed() {
    navigator.clipboard.writeText(this.embedCode());
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 2000);
  }
}
