# PR #3: File Upload + Document Processing

## Overview
Multi-file upload with automatic text extraction and sentence-aware chunking for RAG processing.

## API Endpoints

### POST /api/projects/{projectId}/documents
Upload one or more files (multipart/form-data). Supported types: PDF, DOCX, TXT, MD, CSV.

**Size limits per tier:** Free=50MB, Pro=200MB, Enterprise=500MB per file.

**Response:** `DocumentUploadResponse` with upload results and document statuses.

### GET /api/projects/{projectId}/documents
List all documents in a project.

### GET /api/projects/{projectId}/documents/{id}
Get document details (status, token count, chunk count).

### DELETE /api/projects/{projectId}/documents/{id}
Delete document, its chunks, and the stored file.

### GET /api/projects/{projectId}/documents/{documentId}/chunks
Paginated chunk preview. Query params: `page` (default 1), `pageSize` (default 20, max 100).

## Architecture

### Document Processing Pipeline
1. **Upload** → validate file type & size → save to local storage
2. **Extract** → PDF (PdfPig), DOCX (OpenXml), TXT/MD/CSV (direct read)
3. **Chunk** → recursive sentence-aware splitting (512 tokens, 50 overlap)
4. **Store** → persist chunks as `DocumentChunk` entities

### Interfaces (PipeRAG.Core)
- `IFileStorageService` — file storage abstraction
- `IDocumentProcessor` — text extraction
- `IChunkingService` — text chunking

### Implementations (PipeRAG.Infrastructure)
- `LocalFileStorageService` — stores in `uploads/{projectId}/{documentId}/{filename}`
- `DocumentProcessor` — uses PdfPig + OpenXml
- `ChunkingService` — sentence-aware recursive chunking

## Status Flow
`Uploaded → Processing → Chunked → (Embedded)` or `→ Failed`

## NuGet Packages Added
- `UglyToad.PdfPig` — PDF text extraction
- `DocumentFormat.OpenXml` — DOCX text extraction
- `FluentAssertions` — test assertions
