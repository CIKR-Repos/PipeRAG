# PipeRAG Feature Backlog

## Phase 1 — Foundation (PRs #1-#6)

### PR #1: Project Setup + Database Schema + EF Core
- PostgreSQL + pgvector setup via EF Core
- Entity models: Users, Organizations, Projects, Documents, Pipelines, ChatMessages
- Database migrations
- Health check endpoint
- Docker Compose verified working

### PR #2: Authentication + User Management
- ASP.NET Identity with JWT tokens
- Register/Login/Refresh endpoints
- User tiers (free/pro/enterprise)
- Rate limiting middleware per tier
- Angular auth service + login/register pages (Signals)

### PR #3: File Upload + Document Processing
- Multi-file upload endpoint (PDF, DOCX, TXT, MD, CSV)
- File validation + virus scanning placeholder
- Document chunking service (recursive, sentence-level)
- Chunk preview API
- Storage abstraction (local filesystem for MVP)

### PR #4: RAG Pipeline Engine (Semantic Kernel)
- Semantic Kernel integration
- Embedding generation + pgvector storage
- Auto-pipeline: upload → chunk → embed → store (zero config)
- Model router (tier-gated: free=small models, pro=large models)
- Pipeline configuration CRUD

### PR #5: Chat / Query Engine
- Chat endpoint with streaming (SSE)
- Retrieval: semantic search via pgvector (+ hybrid for Pro)
- Conversation memory (sliding window + summarization)
- Chat history storage + retrieval API
- Angular chat UI component (Signals + streaming)

### PR #6: Pipeline Builder UI
- Angular visual pipeline builder (drag & drop blocks)
- 5 block types: Source → Chunking → Embedding → Retrieval → Generation
- React Flow equivalent using Angular CDK drag-drop
- Pipeline save/load
- Real-time chunk preview panel

## Phase 2 — Ship It (PRs #7-#10)

### PR #7: Embeddable Chat Widget
- Standalone JS widget (`<script>` tag embed)
- Customizable theme (colors, position, avatar)
- Widget configuration API
- Sandboxed iframe from separate origin

### PR #8: Dashboard + Analytics
- Project dashboard (usage, queries, costs)
- Query analytics (popular questions, failed retrievals)
- Angular dashboard with charts (signals-driven)

### PR #9: Billing + Tier Enforcement
- Stripe integration
- Free/Pro/Enterprise tier limits enforcement
- Usage tracking + quota management
- Upgrade/downgrade flows

### PR #10: Deploy + CI/CD
- Dockerfile for API
- GitHub Actions CI/CD pipeline
- fly.io deployment config
- Production-ready configuration
