import { Component, inject, signal, computed, OnInit, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ChatService, ChatMessage, ChatSession } from './chat.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="chat-layout">
      <!-- Sidebar: Session List -->
      <aside class="chat-sidebar">
        <div class="sidebar-header">
          <h3>Chat Sessions</h3>
          <button class="btn-new" (click)="newSession()">+ New</button>
        </div>
        <div class="session-list">
          @for (session of chatService.sessions(); track session.id) {
            <div class="session-item"
                 [class.active]="chatService.currentSessionId() === session.id"
                 (click)="selectSession(session)">
              <span class="session-title">{{ session.title || 'Untitled' }}</span>
              <span class="session-meta">{{ session.messageCount }} msgs</span>
              <button class="btn-delete" (click)="deleteSession($event, session.id)">×</button>
            </div>
          }
        </div>
      </aside>

      <!-- Main Chat Area -->
      <main class="chat-main">
        <div class="messages-container" #messagesContainer>
          @if (chatService.messages().length === 0) {
            <div class="empty-state">
              <h2>Ask anything about your documents</h2>
              <p>Your uploaded documents will be used as context for answers.</p>
            </div>
          }
          @for (msg of chatService.messages(); track msg.id) {
            <div class="message" [class]="'message-' + msg.role.toLowerCase()">
              <div class="message-role">{{ msg.role === 'User' ? 'You' : 'Assistant' }}</div>
              <div class="message-content">
                {{ msg.content }}
                @if (msg.isStreaming) {
                  <span class="cursor-blink">▊</span>
                }
              </div>
              @if (msg.sources && msg.sources.length > 0) {
                <div class="sources">
                  <details>
                    <summary>{{ msg.sources.length }} source(s) used</summary>
                    @for (source of msg.sources; track source.documentId + source.score) {
                      <div class="source-item">
                        <strong>{{ source.documentName }}</strong>
                        <span class="score">{{ (source.score * 100).toFixed(0) }}%</span>
                        <p>{{ source.chunkContent | slice:0:200 }}...</p>
                      </div>
                    }
                  </details>
                </div>
              }
              @if (msg.tokensUsed) {
                <div class="tokens-used">{{ msg.tokensUsed }} tokens</div>
              }
            </div>
          }
        </div>

        <!-- Input Area -->
        <div class="input-area">
          <textarea
            [(ngModel)]="inputMessage"
            (keydown.enter)="onEnter($event)"
            placeholder="Type your question..."
            rows="2"
            [disabled]="chatService.isLoading()"
          ></textarea>
          <button
            class="btn-send"
            (click)="sendMessage()"
            [disabled]="chatService.isLoading() || !inputMessage().trim()">
            {{ chatService.isLoading() ? '...' : 'Send' }}
          </button>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .chat-layout { display: flex; height: calc(100vh - 64px); }
    .chat-sidebar {
      width: 260px; border-right: 1px solid #e0e0e0; display: flex; flex-direction: column;
      background: #f8f9fa;
    }
    .sidebar-header {
      display: flex; justify-content: space-between; align-items: center; padding: 16px;
      border-bottom: 1px solid #e0e0e0;
    }
    .sidebar-header h3 { margin: 0; font-size: 14px; }
    .btn-new {
      background: #4f46e5; color: white; border: none; padding: 6px 12px;
      border-radius: 6px; cursor: pointer; font-size: 12px;
    }
    .session-list { flex: 1; overflow-y: auto; }
    .session-item {
      padding: 12px 16px; cursor: pointer; border-bottom: 1px solid #eee; position: relative;
    }
    .session-item:hover { background: #e8e8e8; }
    .session-item.active { background: #e0e7ff; }
    .session-title { display: block; font-size: 13px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .session-meta { font-size: 11px; color: #888; }
    .btn-delete {
      position: absolute; right: 8px; top: 8px; background: none; border: none;
      color: #999; cursor: pointer; font-size: 16px;
    }
    .chat-main { flex: 1; display: flex; flex-direction: column; }
    .messages-container { flex: 1; overflow-y: auto; padding: 24px; }
    .empty-state { text-align: center; margin-top: 20%; color: #666; }
    .message { margin-bottom: 16px; max-width: 80%; }
    .message-user { margin-left: auto; }
    .message-assistant { margin-right: auto; }
    .message-role { font-size: 11px; color: #888; margin-bottom: 4px; }
    .message-content {
      padding: 12px 16px; border-radius: 12px; line-height: 1.5; white-space: pre-wrap;
    }
    .message-user .message-content { background: #4f46e5; color: white; }
    .message-assistant .message-content { background: #f0f0f0; color: #333; }
    .cursor-blink { animation: blink 1s infinite; }
    @keyframes blink { 50% { opacity: 0; } }
    .sources { margin-top: 8px; font-size: 12px; }
    .source-item { padding: 8px; background: #fafafa; border-radius: 6px; margin-top: 4px; }
    .source-item .score { color: #4f46e5; margin-left: 8px; }
    .tokens-used { font-size: 11px; color: #aaa; margin-top: 4px; }
    .input-area { display: flex; gap: 8px; padding: 16px; border-top: 1px solid #e0e0e0; }
    .input-area textarea {
      flex: 1; resize: none; border: 1px solid #ddd; border-radius: 8px;
      padding: 10px 14px; font-size: 14px; font-family: inherit;
    }
    .btn-send {
      background: #4f46e5; color: white; border: none; padding: 10px 20px;
      border-radius: 8px; cursor: pointer; font-weight: 600;
    }
    .btn-send:disabled { opacity: 0.5; cursor: not-allowed; }
  `]
})
export class ChatComponent implements OnInit, AfterViewChecked {
  chatService = inject(ChatService);
  private route = inject(ActivatedRoute);

  inputMessage = signal('');
  projectId = signal('');

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

  private scrollToBottom(): void {
    if (this.messagesContainer) {
      const el = this.messagesContainer.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }
}
