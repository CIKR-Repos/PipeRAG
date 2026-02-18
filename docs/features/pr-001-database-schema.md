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
| DocumentChunk | Text chunks with vector(1536) embeddings for semantic search |
| Pipeline | RAG pipeline configuration |
| PipelineRun | Pipeline execution history |
| ChatSession | Chat conversation container |
| ChatMessage | Individual chat messages (User/Assistant/System) |
| ApiKey | Programmatic access keys |
| AuditLog | Action audit trail |

## Key Design Decisions
- **pgvector** with 1536-dimension vectors (OpenAI ada-002 compatible)
- Enum values stored as strings for readability
- Unique indexes on User.Email, Organization.Slug, ApiKey.KeyHash
- Cascade deletes on DocumentChunk and ChatMessage
- All timestamps default to UTC

## Health Check
`GET /health` returns:
```json
{ "status": "healthy", "database": "connected", "timestamp": "2026-02-17T..." }
```

## Connection String
Configured in `appsettings.json` → `ConnectionStrings:DefaultConnection`
