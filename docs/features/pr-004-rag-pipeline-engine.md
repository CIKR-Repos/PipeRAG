# PR #4: RAG Pipeline Engine (Semantic Kernel)

## Overview
Integrates Microsoft Semantic Kernel to power the core RAG pipeline: embedding generation, vector storage, and automated document processing.

## Architecture

### Pipeline Flow
```
Upload → Chunk → Embed → Store (pgvector) → Ready for Query
```

### Services
- **ModelRouterService** — Selects AI models based on user tier (Free/Pro/Enterprise)
- **EmbeddingService** — Generates embeddings via Semantic Kernel's OpenAI connector
- **AutoPipelineService** — Orchestrates the full pipeline (chunk → embed → index)
- **PipelineBackgroundService** — Processes pipeline runs asynchronously via channel/queue
- **PipelineRunChannel** — Bounded channel for async pipeline run queuing

### Model Selection by Tier
| Tier | Embedding Model | Chat Model | Max Tokens | Max Docs |
|------|----------------|------------|------------|----------|
| Free | text-embedding-3-small (1536d) | gpt-4.1-mini | 4,096 | 50 |
| Pro | text-embedding-3-large (3072d) | gpt-4.1 | 8,192 | 500 |
| Enterprise | text-embedding-3-large (3072d) | gpt-4.1 | 16,384 | 5,000 |

## API Endpoints

### Pipeline CRUD
- `POST /api/projects/{projectId}/pipelines` — Create pipeline
- `GET /api/projects/{projectId}/pipelines` — List pipelines (auto-creates default)
- `GET /api/projects/{projectId}/pipelines/{id}` — Get pipeline
- `PUT /api/projects/{projectId}/pipelines/{id}` — Update pipeline

### Pipeline Runs
- `POST /api/projects/{projectId}/pipelines/{pipelineId}/run` — Trigger run
- `GET /api/projects/{projectId}/pipelines/{pipelineId}/runs` — List runs

### Model Info
- `GET /api/models` — Get model selection for current user's tier

## Pipeline Configuration
```json
{
  "chunkSize": 512,
  "chunkOverlap": 50,
  "embeddingModel": "text-embedding-3-small",
  "retrievalStrategy": "semantic",
  "topK": 5,
  "scoreThreshold": 0.7
}
```

## Background Processing
Uses `System.Threading.Channels` with a bounded channel (capacity 100) to queue pipeline runs. A `BackgroundService` reads from the channel and processes runs sequentially, updating status in the database.

## Testing
- **ModelRouterTests** — Verifies tier → model mapping for all tiers
- **PipelineCrudTests** — CRUD operations on pipelines and runs
- **AutoPipelineTests** — End-to-end pipeline orchestration with mock embedding service
