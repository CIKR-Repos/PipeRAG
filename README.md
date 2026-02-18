# PipeRAG 🔧

**No-code RAG pipeline builder for non-technical users.**

Upload files → Get a chatbot in 30 seconds. Zero code. Zero config.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | .NET 10 + ASP.NET Core Web API |
| **RAG Engine** | Microsoft Semantic Kernel |
| **Frontend** | Angular 21 + Signals (zoneless) |
| **Vector DB** | pgvector (PostgreSQL extension — free) |
| **Database** | PostgreSQL 16 |
| **Cache** | Redis |
| **Auth** | ASP.NET Identity + JWT |
| **File Storage** | Local / S3-compatible |

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│                    Angular 21 SPA                         │
│  (Signals · Reactive Forms · Pipeline Builder · Chat UI) │
└──────────────┬───────────────────────────────────────────┘
               │ REST API (JSON)
┌──────────────▼───────────────────────────────────────────┐
│              ASP.NET Core Web API (.NET 10)               │
│  ┌─────────────┐ ┌──────────────┐ ┌──────────────────┐  │
│  │ Auth Module  │ │Pipeline Svc  │ │  Chat/Query Svc  │  │
│  └─────────────┘ └──────┬───────┘ └────────┬─────────┘  │
│                          │                  │            │
│  ┌───────────────────────▼──────────────────▼─────────┐  │
│  │           Microsoft Semantic Kernel                 │  │
│  │  (Embeddings · Chunking · Retrieval · Generation)   │  │
│  └─────────────────┬────────────────┬─────────────────┘  │
│                    │                │                     │
│  ┌─────────────────▼──┐  ┌─────────▼──────────────────┐  │
│  │  PostgreSQL + pgvec │  │  Redis Cache               │  │
│  │  (data + vectors)   │  │  (sessions · rate limits)  │  │
│  └─────────────────────┘  └────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```

## Project Structure

```
piperag/
├── src/
│   ├── PipeRAG.Api/           # ASP.NET Core Web API
│   ├── PipeRAG.Core/          # Domain models, interfaces, DTOs
│   └── PipeRAG.Infrastructure/ # EF Core, Semantic Kernel, storage
├── client/                    # Angular 21 SPA
├── tests/
│   └── PipeRAG.Tests/         # xUnit tests
├── docs/                      # Feature documentation
└── docker-compose.yml         # PostgreSQL + Redis + API
```

## Development

```bash
# Backend
cd src/PipeRAG.Api && dotnet run

# Frontend
cd client && ng serve

# Database (Docker)
docker-compose up -d postgres redis
```

## Feature Development

Every feature follows this workflow:
1. Feature branch from `main`
2. Implementation + tests + documentation in `docs/`
3. PR to `main` for review
4. Merge after approval

## License

MIT
