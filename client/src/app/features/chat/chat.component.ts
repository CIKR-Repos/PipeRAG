import { Component, inject, signal, computed, OnInit, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ChatService, ChatMessage, ChatSession } from './chat.service';
import { NavbarComponent } from '../../shared/components/navbar/navbar';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [FormsModule, NavbarComponent, RouterLink],
  template: `
    <app-navbar />
    <div class="flex h-[calc(100vh-57px)]">

      <!-- Sidebar -->
      <aside class="w-72 border-r border-border bg-surface-0 flex flex-col shrink-0
                    max-md:absolute max-md:z-40 max-md:h-[calc(100vh-57px)] max-md:transition-transform max-md:duration-300"
             [class.max-md:-translate-x-full]="!sidebarOpen()"
             [class.max-md:translate-x-0]="sidebarOpen()">

        <!-- Sidebar header -->
        <div class="flex items-center justify-between p-4 border-b border-border">
          <h3 class="text-sm font-semibold text-text-primary">Sessions</h3>
          <div class="flex items-center gap-2">
            <button (click)="newSession()"
                    class="px-3 py-1.5 bg-accent text-white text-xs font-medium rounded-lg
                           hover:bg-accent-hover transition-colors cursor-pointer border-none">
              + New
            </button>
            <button (click)="sidebarOpen.set(false)"
                    class="md:hidden p-1 text-text-tertiary hover:text-text-primary
                           bg-transparent border-none cursor-pointer">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        <!-- Session list -->
        <div class="flex-1 overflow-y-auto scrollbar-thin">
          @for (session of chatService.sessions(); track session.id) {
            <div class="px-4 py-3 cursor-pointer border-b border-border/30 relative group
                        transition-colors duration-150"
                 [class]="chatService.currentSessionId() === session.id
                           ? 'bg-accent/10 border-l-2 border-l-accent'
                           : 'hover:bg-surface-2'"
                 (click)="selectSession(session); sidebarOpen.set(false)">
              <span class="block text-sm text-text-primary truncate pr-6">
                {{ session.title || 'Untitled' }}
              </span>
              <span class="text-xs text-text-tertiary">{{ session.messageCount }} messages</span>
              <button class="absolute right-3 top-3 opacity-0 group-hover:opacity-100
                             text-text-tertiary hover:text-red-400 transition-all
                             bg-transparent border-none cursor-pointer p-1"
                      (click)="deleteSession($event, session.id)">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          }
          @if (chatService.sessions().length === 0) {
            <div class="p-6 text-center text-text-muted text-sm">
              No sessions yet
            </div>
          }
        </div>

        <!-- Back to dashboard -->
        <div class="p-3 border-t border-border">
          <a [routerLink]="['/dashboard']"
             class="flex items-center gap-2 px-3 py-2 text-text-tertiary hover:text-text-primary
                    hover:bg-surface-2 rounded-lg transition-colors text-sm no-underline">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            Dashboard
          </a>
        </div>
      </aside>

      <!-- Mobile sidebar overlay -->
      @if (sidebarOpen()) {
        <div class="md:hidden fixed inset-0 top-[57px] bg-black/40 z-30"
             (click)="sidebarOpen.set(false)"></div>
      }

      <!-- Main Chat Area -->
      <main class="flex-1 flex flex-col min-w-0 bg-surface-1">

        <!-- Chat header (mobile) -->
        <div class="md:hidden flex items-center gap-3 px-4 py-3 border-b border-border bg-surface-0/80 backdrop-blur-xl">
          <button (click)="sidebarOpen.set(true)"
                  class="p-1.5 text-text-secondary hover:text-text-primary
                         bg-transparent border-none cursor-pointer">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
          <span class="text-sm font-medium text-text-primary">Chat</span>
        </div>

        <!-- Messages container -->
        <div class="flex-1 overflow-y-auto px-4 md:px-8 py-6 scrollbar-thin" #messagesContainer>

          @if (chatService.messages().length === 0 && !chatService.isLoading()) {
            <!-- Empty state -->
            <div class="flex items-center justify-center h-full">
              <div class="text-center max-w-md">
                <div class="w-16 h-16 rounded-2xl bg-accent/10 flex items-center justify-center
                            text-3xl mx-auto mb-6">&#128172;</div>
                <h2 class="text-xl font-bold text-text-primary mb-2">
                  Ask anything about your documents
                </h2>
                <p class="text-text-secondary text-sm leading-relaxed">
                  Your uploaded documents will be used as context for accurate, sourced answers.
                </p>
              </div>
            </div>
          }

          @for (msg of chatService.messages(); track msg.id) {
            <div class="flex gap-3 mb-5 max-w-3xl"
                 [class]="msg.role === 'User' ? 'ml-auto flex-row-reverse' : ''">
              <!-- Avatar -->
              <div class="w-8 h-8 rounded-full shrink-0 flex items-center justify-center text-xs font-semibold"
                   [class]="msg.role === 'User'
                             ? 'bg-accent text-white'
                             : 'bg-surface-3 text-text-secondary'">
                {{ msg.role === 'User' ? 'You' : 'AI' }}
              </div>

              <div class="max-w-[80%] min-w-0">
                <!-- Bubble -->
                <div class="px-4 py-3 rounded-2xl text-sm leading-relaxed whitespace-pre-wrap break-words"
                     [class]="msg.role === 'User'
                               ? 'bg-accent text-white rounded-br-md'
                               : 'bg-surface-2 text-text-primary border border-border rounded-bl-md'">
                  {{ msg.content }}
                  @if (msg.isStreaming) {
                    <span class="inline-block w-1.5 h-4 bg-accent ml-1 animate-pulse rounded-sm align-middle"></span>
                  }
                </div>

                <!-- Sources -->
                @if (msg.sources && msg.sources.length > 0) {
                  <div class="mt-2">
                    <button class="flex items-center gap-1.5 text-xs font-medium text-accent
                                   hover:text-accent-hover transition-colors bg-transparent
                                   border-none cursor-pointer p-0"
                            (click)="toggleSources(msg.id)">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round"
                              d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
                      </svg>
                      {{ msg.sources.length }} source(s)
                      <svg class="w-3 h-3 transition-transform duration-200"
                           [class.rotate-180]="expandedSources().has(msg.id)"
                           fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M19 9l-7 7-7-7" />
                      </svg>
                    </button>
                    @if (expandedSources().has(msg.id)) {
                      <div class="mt-2 space-y-2 animate-slide-up">
                        @for (source of msg.sources; track source.documentId + source.score) {
                          <div class="bg-surface-2 border border-border rounded-xl p-3">
                            <div class="flex items-center justify-between mb-1.5">
                              <span class="text-xs font-semibold text-text-primary truncate mr-2">
                                {{ source.documentName }}
                              </span>
                              <span class="text-xs text-accent font-mono shrink-0">
                                {{ (source.score * 100).toFixed(0) }}%
                              </span>
                            </div>
                            <p class="text-xs text-text-secondary leading-relaxed line-clamp-3">
                              {{ source.chunkContent }}
                            </p>
                          </div>
                        }
                      </div>
                    }
                  </div>
                }

                <!-- Tokens -->
                @if (msg.tokensUsed) {
                  <p class="text-xs text-text-muted mt-1.5">{{ msg.tokensUsed }} tokens</p>
                }
              </div>
            </div>
          }

          <!-- Typing indicator -->
          @if (chatService.isLoading() && lastMessageIsStreaming()) {
            <div class="flex gap-3 mb-5">
              <div class="w-8 h-8 rounded-full bg-surface-3 text-text-secondary
                          flex items-center justify-center text-xs font-semibold shrink-0">AI</div>
              <div class="bg-surface-2 border border-border rounded-2xl rounded-bl-md px-4 py-3">
                <div class="flex gap-1.5">
                  <span class="w-2 h-2 rounded-full bg-text-tertiary animate-bounce" style="animation-delay: 0ms"></span>
                  <span class="w-2 h-2 rounded-full bg-text-tertiary animate-bounce" style="animation-delay: 150ms"></span>
                  <span class="w-2 h-2 rounded-full bg-text-tertiary animate-bounce" style="animation-delay: 300ms"></span>
                </div>
              </div>
            </div>
          }
        </div>

        <!-- Input Area -->
        <div class="p-4 border-t border-border bg-surface-0/80 backdrop-blur-xl">
          <div class="flex gap-3 items-end max-w-3xl mx-auto">
            <textarea
              [(ngModel)]="inputMessage"
              (keydown.enter)="onEnter($event)"
              placeholder="Type your question..."
              rows="1"
              [disabled]="chatService.isLoading()"
              class="flex-1 resize-none bg-surface-2 border border-border rounded-xl px-4 py-3
                     text-text-primary text-sm placeholder:text-text-muted
                     focus:outline-none focus:border-accent focus:ring-2 focus:ring-accent/20
                     transition-all duration-200 max-h-32 scrollbar-thin"></textarea>
            <button (click)="sendMessage()"
                    [disabled]="chatService.isLoading() || !inputMessage().trim()"
                    class="w-10 h-10 shrink-0 flex items-center justify-center rounded-xl
                           bg-accent text-white hover:bg-accent-hover transition-colors
                           cursor-pointer border-none
                           disabled:opacity-40 disabled:cursor-not-allowed">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round"
                      d="M12 19V5m0 0l-7 7m7-7l7 7" />
              </svg>
            </button>
          </div>
        </div>
      </main>
    </div>
  `,
})
export class ChatComponent implements OnInit, AfterViewChecked {
  chatService = inject(ChatService);
  private route = inject(ActivatedRoute);

  inputMessage = signal('');
  projectId = signal('');
  sidebarOpen = signal(false);
  expandedSources = signal(new Set<string>());

  lastMessageIsStreaming = computed(() => {
    const msgs = this.chatService.messages();
    if (msgs.length === 0) return false;
    const last = msgs[msgs.length - 1];
    return last.role === 'Assistant' && last.content === '' && last.isStreaming;
  });

  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;
  private shouldScroll = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('projectId') ?? '';
    this.projectId.set(id);
    if (id) {
      this.chatService.loadSessions(id);
    }
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  async sendMessage(): Promise<void> {
    const msg = this.inputMessage().trim();
    if (!msg) return;
    this.inputMessage.set('');
    this.shouldScroll = true;
    await this.chatService.sendMessageStream(this.projectId(), msg);
  }

  onEnter(event: Event): void {
    const ke = event as KeyboardEvent;
    if (!ke.shiftKey) {
      ke.preventDefault();
      this.sendMessage();
    }
  }

  async selectSession(session: ChatSession): Promise<void> {
    await this.chatService.loadMessages(this.projectId(), session.id);
  }

  async deleteSession(event: Event, sessionId: string): Promise<void> {
    event.stopPropagation();
    await this.chatService.deleteSession(this.projectId(), sessionId);
  }

  newSession(): void {
    this.chatService.newSession();
  }

  toggleSources(msgId: string): void {
    this.expandedSources.update(s => {
      const next = new Set(s);
      if (next.has(msgId)) next.delete(msgId);
      else next.add(msgId);
      return next;
    });
  }

  private scrollToBottom(): void {
    if (this.messagesContainer) {
      const el = this.messagesContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }
}
