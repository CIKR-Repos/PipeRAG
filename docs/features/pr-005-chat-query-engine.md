# PR #5: Chat / Query Engine

## Overview
Implements the RAG chat/query engine with streaming responses, conversation memory, and an Angular chat UI.

## Features

### Chat Controller (`/api/projects/{projectId}/chat`)
- **POST /** — Send message, get AI response
- **POST /stream** — SSE streaming response (text/event-stream)
- **GET /sessions** — List chat sessions
- **GET /sessions/{sessionId}/messages** — Get chat history
- **DELETE /sessions/{sessionId}** — Delete session

All endpoints require authentication and project ownership verification.

### Query Engine Service
- **Flow:** User query → generate embedding → pgvector cosine distance search → build context prompt → LLM generation
- **Retrieval strategies:**
  - `similarity` (Free tier) — Pure semantic search via pgvector `<=>` operator
  - `hybrid` (Pro/Enterprise) — Semantic + keyword fallback
- **Configurable:** top-k (default 5), score threshold (default 0.7)
- **Source references** returned with each response (document name, chunk content, similarity score)

### Conversation Memory
- **Sliding window:** Keeps last N messages (default 10) in LLM context
- **Summarization:** When message count exceeds window, older messages are summarized
- **Persistence:** All messages stored in ChatMessage entity
- **Session management:** Auto-create sessions, list, delete

### Streaming Support
- Server-Sent Events (SSE) via `text/event-stream`
- Tokens streamed as they arrive from Semantic Kernel's `GetStreamingChatMessageContentsAsync`
- Final chunk includes source references and token count

### Angular Chat UI
- Standalone component with Angular Signals (no RxJS Subjects)
- Session sidebar with create/select/delete
- Message bubbles (user/assistant) with real-time streaming display
- Source references expandable under assistant messages
- Fetch API with ReadableStream for SSE consumption
- Route: `/projects/:projectId/chat`

## DTOs
- `ChatRequest` — message, sessionId?, retrievalStrategy?, topK?
- `ChatResponse` — message, sessionId, sources[], tokensUsed
- `ChatStreamChunk` — content, done, sessionId, sources?, tokensUsed?
- `ChatSessionResponse` — id, title, messageCount, createdAt, updatedAt
- `ChatMessageResponse` — id, role, content, sources, tokensUsed, createdAt
- `SourceReference` — documentId, documentName, chunkContent, score

## Tests (17 new tests)
- **ConversationMemoryTests** (10): session CRUD, sliding window, summarization, message storage
- **QueryEngineTests** (5): model routing, embedding mock, memory integration
- **ChatControllerTests** (7): auth checks, chat endpoint, session management

## Files Changed
- `src/PipeRAG.Core/DTOs/ChatDtos.cs`
- `src/PipeRAG.Core/Interfaces/IQueryEngineService.cs`
- `src/PipeRAG.Core/Interfaces/IConversationMemoryService.cs`
- `src/PipeRAG.Infrastructure/Services/QueryEngineService.cs`
- `src/PipeRAG.Infrastructure/Services/ConversationMemoryService.cs`
- `src/PipeRAG.Api/Controllers/ChatController.cs`
- `src/PipeRAG.Api/Program.cs` (DI registration)
- `client/src/app/features/chat/chat.service.ts`
- `client/src/app/features/chat/chat.component.ts`
- `client/src/app/app.routes.ts`
- `tests/PipeRAG.Tests/QueryEngineTests.cs`
- `tests/PipeRAG.Tests/ConversationMemoryTests.cs`
- `tests/PipeRAG.Tests/ChatControllerTests.cs`
