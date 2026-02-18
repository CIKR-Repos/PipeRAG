# PR #1: Database Schema + EF Core Setup

## Overview
Initial database schema with 12 entity models, EF Core with PostgreSQL + pgvector for vector embeddings.

## Entities
| Entity | Purpose |
|--------|---------|
| User | Application users with tier (Free/Pro/Enterprise) |
| Organization | Groups users and projects |
| OrgMembership | User↔Organization join with roles (Member/Admin/Owner) |
| Project | RAG project containing documents and pipelines |
| Document | Uploaded files with processing status |
| DocumentChunk | Text chunks with configurable-dimension embeddings for semantic search |
| Pipeline | RAG pipeline configuration |
| PipelineRun | Pipeline execution history |
| ChatSession | Chat conversation container |
| ChatMessage | Individual chat messages (User/Assistant/System) |
| ApiKey | Programmatic access keys |
| AuditLog | Action audit trail |

## Key Design Decisions
- **pgvector** with configurable vector dimensions (default 1536 for text-embedding-3-small; 3072 for text-embedding-3-large)
- Enum values stored as strings for readability
- Unique indexes on User.Email, Organization.Slug, ApiKey.KeyHash
- Cascade deletes on DocumentChunk and ChatMessage
- All timestamps default to UTC

## Configuration

### Connection String Setup
The connection string is **not** stored in `appsettings.json` for security. Configure it via:

1. **User Secrets** (recommended for development):
   ```bash
   cd src/PipeRAG.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=piperag;Username=piperag;Password=your_password"
   ```

2. **Environment Variables**:
   ```bash
   export ConnectionStrings__DefaultConnection="Host=localhost;Database=piperag;Username=piperag;Password=your_password"
   ```

3. **appsettings.Development.json** (gitignored):
   See `appsettings.Development.json.example` for the expected format.

### Embedding Dimension
Configure the vector embedding dimension in appsettings or environment:
```json
{
  "Embedding": {
    "Dimension": 1536
  }
}
```
- `1536` — OpenAI text-embedding-3-small (default)
- `3072` — OpenAI text-embedding-3-large

> **Note:** Changing the dimension after data has been stored requires a migration and re-embedding.

## Health Check
`GET /health` returns:

**Healthy (200):**
```json
{ "status": "healthy", "database": true, "timestamp": "2026-02-17T..." }
```

**Unhealthy (503):**
```json
{ "status": "unhealthy", "database": false, "error": "...", "timestamp": "2026-02-17T..." }
```
