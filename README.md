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
# Start infrastructure
docker-compose up -d postgres redis

# Backend
cd src/PipeRAG.Api && dotnet run

# Frontend
cd client && ng serve

# Run tests
dotnet test tests/PipeRAG.Tests/PipeRAG.Tests.csproj
```

## Deployment

### Docker

```bash
# Build the image
docker build -t piperag .

# Run with Docker Compose (production profile)
JWT_SECRET=your-secret OPENAI_API_KEY=sk-... docker-compose --profile production up -d
```

### fly.io

```bash
# First-time setup
fly launch --no-deploy
fly secrets set JWT_SECRET=your-secret
fly secrets set OPENAI_API_KEY=sk-...
fly secrets set DATABASE_URL=postgres://...
fly secrets set STRIPE_SECRET_KEY=sk_...

# Deploy
fly deploy
```

### CI/CD

The GitHub Actions pipeline (`.github/workflows/ci.yml`) runs automatically:

| Trigger | Steps |
|---------|-------|
| **PR to main** | Build → Test → Docker build (no push) |
| **Push to main** | Build → Test → Docker push to GHCR → Deploy to fly.io |

**Required secrets**: `FLY_API_TOKEN` (for fly.io deployment)

### Environment Variables (Production)

| Variable | Description |
|----------|-------------|
| `DATABASE_URL` | PostgreSQL connection string |
| `JWT_SECRET` | JWT signing key (min 32 chars) |
| `OPENAI_API_KEY` | OpenAI API key for embeddings/LLM |
| `STRIPE_SECRET_KEY` | Stripe secret key |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook signing secret |
| `REDIS_URL` | Redis connection string |

## License

MIT
