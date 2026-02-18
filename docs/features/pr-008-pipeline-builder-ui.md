# PR #8 — Pipeline Builder UI

## Overview
Visual pipeline builder with card-based interface for configuring RAG pipeline stages:
Source → Chunking → Embedding → Retrieval → Generation.

## Components
- **PipelineBuilderComponent** — `client/src/app/features/pipeline-builder/pipeline-builder.component.ts`
- **PipelineService** — `client/src/app/features/pipeline-builder/pipeline.service.ts`

## Pipeline Blocks
| Block | Settings |
|-------|----------|
| Source | Document list, file types, total size |
| Chunking | Chunk size (128-2048), overlap (0-200), strategy (recursive/sentence) |
| Embedding | Model selector (tier-gated), dimension display |
| Retrieval | Strategy (semantic/hybrid), top-k (1-20), score threshold (0-1) |
| Generation | Model (tier-gated), temperature, max tokens, system prompt |

## Chunk Preview Panel
Side panel showing real-time chunking preview with chunk count, avg tokens, and first 3 chunks.

## API Endpoints Used
- `GET /api/projects/{id}/pipelines` — Load config
- `PUT /api/projects/{id}/pipelines/{pipelineId}` — Save config
- `GET /api/projects/{id}/documents` — Document list
- `GET /api/projects/{id}/documents/{docId}/chunks` — Chunk preview
- `GET /api/models` — Tier-gated models

## Backend Changes
Extended `PipelineConfigDto` with: ChunkingStrategy, EmbeddingDimensions, GenerationModel, Temperature, MaxTokens, SystemPrompt.

## Route
`/projects/:projectId/pipeline`
