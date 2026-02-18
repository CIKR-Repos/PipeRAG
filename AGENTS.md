# PipeRAG — Agent Instructions

## Project
PipeRAG: No-code RAG pipeline builder. .NET 10 + Angular 21 + Semantic Kernel + pgvector.

## Workflow (STRICT)
1. Create feature branch: `feat/<feature-name>` from `main`
2. Implement the feature with tests
3. Add documentation in `docs/features/<feature-name>.md`
4. Commit with conventional commits: `feat:`, `fix:`, `docs:`, `test:`
5. Push branch and create PR to `main`
6. NEVER merge — PRs are for human review only

## Code Standards
- Backend: C# 13, .NET 10, nullable reference types enabled
- Use dependency injection everywhere
- Repository pattern for data access
- All public APIs need XML doc comments
- Frontend: Angular 21, Signals (not RxJS Subjects), standalone components
- Tests: xUnit + FluentAssertions for backend, Jasmine for Angular
- Every feature needs at least basic test coverage

## Architecture
- `PipeRAG.Core` — Domain entities, interfaces, DTOs (NO infrastructure deps)
- `PipeRAG.Infrastructure` — EF Core, Semantic Kernel, external services
- `PipeRAG.Api` — Controllers, middleware, DI configuration
- `client/` — Angular 21 SPA

## Database
- PostgreSQL 16 + pgvector extension
- EF Core with code-first migrations
- Connection string in appsettings.json

## Key Libraries
- Microsoft.SemanticKernel (RAG orchestration)
- Npgsql.EntityFrameworkCore.PostgreSQL + pgvector
- Microsoft.AspNetCore.Identity (auth)
- FluentValidation (request validation)
