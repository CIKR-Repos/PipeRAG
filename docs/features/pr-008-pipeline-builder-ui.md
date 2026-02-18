# PR #8 - Pipeline Builder UI

Visual pipeline builder with card-based interface for configuring RAG pipeline stages:
Source -> Chunking -> Embedding -> Retrieval -> Generation.

## Components
- **PipelineBuilderComponent** - Standalone Angular component with expandable block cards
- **PipelineService** - Signal-based state management for pipeline config

## Pipeline Blocks
| Block | Settings |
|-------|----------|
| Source | Document list, file types, total size |
| Chunking | Chunk size (128-2048), overlap (0-200), strategy |
| Embedding | Model selector (tier-gated), dimension display |
| Retrieval | Strategy (semantic/hybrid), top-k (1-20), score threshold |
| Generation | Model (tier-gated), temperature, max tokens, system prompt |

## Chunk Preview Panel
Side panel with real-time chunking preview (chunk count, avg tokens, first 3 chunks).

## Backend Changes
Extended PipelineConfigDto with: ChunkingStrategy, EmbeddingDimensions, GenerationModel, Temperature, MaxTokens, SystemPrompt.
