import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

export interface ChatMessage {
  id: string;
  role: 'User' | 'Assistant' | 'System';
  content: string;
  sources?: SourceReference[];
  tokensUsed?: number;
  createdAt: string;
  isStreaming?: boolean;
}

export interface SourceReference {
  documentId: string;
  documentName: string;
  chunkContent: string;
  score: number;
}

export interface ChatSession {
  id: string;
  title: string;
  messageCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface ChatResponse {
  message: string;
  sessionId: string;
  sources: SourceReference[];
  tokensUsed: number;
}

export interface ChatStreamChunk {
  content: string;
  done: boolean;
  sessionId: string;
  sources?: SourceReference[];
  tokensUsed?: number;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  readonly sessions = signal<ChatSession[]>([]);
  readonly messages = signal<ChatMessage[]>([]);
  readonly currentSessionId = signal<string | null>(null);
  readonly isLoading = signal(false);

  async loadSessions(projectId: string): Promise<void> {
    const sessions = await firstValueFrom(
      this.http.get<ChatSession[]>(`/api/projects/${projectId}/chat/sessions`)
    );
    this.sessions.set(sessions);
  }

  async loadMessages(projectId: string, sessionId: string): Promise<void> {
    const messages = await firstValueFrom(
      this.http.get<ChatMessage[]>(`/api/projects/${projectId}/chat/sessions/${sessionId}/messages`)
    );
    this.messages.set(messages);
    this.currentSessionId.set(sessionId);
  }

  async deleteSession(projectId: string, sessionId: string): Promise<void> {
    await firstValueFrom(
      this.http.delete(`/api/projects/${projectId}/chat/sessions/${sessionId}`)
    );
    this.sessions.update(s => s.filter(x => x.id !== sessionId));
    if (this.currentSessionId() === sessionId) {
      this.currentSessionId.set(null);
      this.messages.set([]);
    }
  }

  async sendMessageStream(projectId: string, message: string, sessionId?: string): Promise<void> {
    this.isLoading.set(true);

    // Add user message optimistically
    const userMsg: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'User',
      content: message,
      createdAt: new Date().toISOString()
    };
    this.messages.update(msgs => [...msgs, userMsg]);

    // Add placeholder for assistant
    const assistantMsg: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'Assistant',
      content: '',
      createdAt: new Date().toISOString(),
      isStreaming: true
    };
    this.messages.update(msgs => [...msgs, assistantMsg]);

    try {
      const body = JSON.stringify({
        message,
        sessionId: sessionId ?? this.currentSessionId(),
        retrievalStrategy: 'similarity',
        topK: 5
      });

      const token = this.auth.getToken();
      const response = await fetch(`/api/projects/${projectId}/chat/stream`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body
      });

      if (!response.ok || !response.body) throw new Error('Stream failed');

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() ?? '';

        for (const line of lines) {
          if (!line.startsWith('data: ')) continue;
          const json = line.slice(6).trim();
          if (!json) continue;

          const chunk: ChatStreamChunk = JSON.parse(json);

          if (!this.currentSessionId()) {
            this.currentSessionId.set(chunk.sessionId);
          }

          if (chunk.done) {
            this.messages.update(msgs =>
              msgs.map(m => m.id === assistantMsg.id
                ? { ...m, isStreaming: false, sources: chunk.sources, tokensUsed: chunk.tokensUsed }
                : m)
            );
          } else {
            this.messages.update(msgs =>
              msgs.map(m => m.id === assistantMsg.id
                ? { ...m, content: m.content + chunk.content }
                : m)
            );
          }
        }
      }

      // Refresh sessions list
      await this.loadSessions(projectId);
    } catch (err) {
      this.messages.update(msgs =>
        msgs.map(m => m.id === assistantMsg.id
          ? { ...m, content: 'Error: Failed to get response.', isStreaming: false }
          : m)
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  newSession(): void {
    this.currentSessionId.set(null);
    this.messages.set([]);
  }
}
