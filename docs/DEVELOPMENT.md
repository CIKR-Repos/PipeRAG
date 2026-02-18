# PipeRAG Development Guide

## Project Structure

```
piperag/
├── src/
│   ├── PipeRAG.Api/              # ASP.NET Core Web API (controllers, middleware, DI)
│   ├── PipeRAG.Core/             # Domain layer (entities, interfaces, DTOs, enums)
│   │   ├── Entities/             # EF Core entity models
│   │   ├── Enums/                # Status enums, tiers, roles
│   │   ├── DTOs/                 # Request/Response data transfer objects
│   │   └── Interfaces/           # Service contracts
│   └── PipeRAG.Infrastructure/   # Implementation layer (services, data access)
│       ├── Data/                 # DbContext, migrations
│       └── Services/             # Business logic implementations
├── client/                       # Angular 21 SPA (Signals, standalone components)
│   └── src/app/
│       ├── core/                 # Auth service, interceptors, guards
│       └── features/             # Feature modules (chat, pipeline-builder, etc.)
├── tests/
│   ├── PipeRAG.Tests/            # xUnit unit + integration tests
│   └── PipeRAG.E2E/              # Playwright end-to-end tests
├── docs/
│   ├── ARCHITECTURE.md           # Full architecture document (v8)
│   ├── DEVELOPMENT.md            # This file
│   ├── features/                 # Per-PR feature documentation
│   └── screenshots/              # E2E test screenshots + evidence
├── docker-compose.yml            # PostgreSQL (pgvector) + Redis
├── AGENTS.md                     # Sub-agent development instructions
├── BACKLOG.md                    # Feature backlog (10 PRs)
└── README.md                     # Project overview
```

## Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Backend API | ASP.NET Core | .NET 10 |
| RAG Engine | Microsoft Semantic Kernel | Latest |
| Frontend | Angular + Signals | 21 |
| Vector DB | pgvector (PostgreSQL) | PG 16 |
| Database | PostgreSQL | 16 |
| Cache | Redis | 7 |
| Auth | Custom JWT + BCrypt | - |
| Testing | xUnit + FluentAssertions | Backend |
| E2E Testing | Playwright | Frontend |
| CI/CD | GitHub Actions | - |

## Development Workflow

### Feature Development (Autonomous)
1. Sub-agent spawns on feature branch from `main`
2. Implements feature with tests + documentation
3. Pushes branch, creates PR
4. Sourcery AI auto-reviews
5. Fix agent addresses review comments
6. Human reviews and merges
7. Pipeline cron detects merge, spawns next feature

### Quality Gates
- `dotnet build` must pass (0 errors, 0 warnings)
- `dotnet test` must pass (all green)
- `ng build` must pass
- Playwright E2E tests must pass (with screenshots)
- Sourcery AI review addressed
- Feature documentation in docs/features/

## Local Setup

```bash
# 1. Start infrastructure
docker compose up -d

# 2. Run migrations
cd src/PipeRAG.Api && dotnet ef database update

# 3. Start backend
cd src/PipeRAG.Api && dotnet run

# 4. Start frontend
cd client && ng serve

# 5. Run tests
dotnet test
cd client && ng test
```

## Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| ConnectionStrings__DefaultConnection | PostgreSQL connection string | Yes |
| Jwt__Secret | JWT signing secret (min 32 chars) | Yes (prod) |
| OpenAI__ApiKey | OpenAI API key for embeddings/LLM | Yes |
| Embedding__Dimension | Vector dimension (default: 1536) | No |
