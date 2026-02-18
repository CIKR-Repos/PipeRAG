import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface PipelineConfig {
  chunkSize: number;
  chunkOverlap: number;
  chunkingStrategy: string;
  embeddingModel: string;
  embeddingDimensions: number;
  retrievalStrategy: string;
  topK: number;
  scoreThreshold: number;
  generationModel: string;
  temperature: number;
  maxTokens: number;
  systemPrompt: string;
}

export interface PipelineResponse {
  id: string;
  projectId: string;
  name: string;
  description: string | null;
  config: PipelineConfig;
  status: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface DocumentInfo {
  id: string;
  fileName: string;
  fileType: string;
  fileSize: number;
  status: string;
}

export interface ChunkPreviewItem {
  content: string;
  tokenCount: number;
  index: number;
}

export interface ChunkPreviewResponse {
  chunks: ChunkPreviewItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ModelSelection {
  embeddingModel: string;
  embeddingDimensions: number;
  chatModel: string;
  maxTokensPerRequest: number;
  maxDocumentsPerProject: number;
}

const DEFAULT_CONFIG: PipelineConfig = {
  chunkSize: 512,
  chunkOverlap: 50,
  chunkingStrategy: 'recursive',
  embeddingModel: 'text-embedding-3-small',
  embeddingDimensions: 1536,
  retrievalStrategy: 'semantic',
  topK: 5,
  scoreThreshold: 0.7,
  generationModel: 'gpt-4o-mini',
  temperature: 0.7,
  maxTokens: 2048,
  systemPrompt: 'You are a helpful assistant. Answer questions based on the provided context.',
};

@Injectable({ providedIn: 'root' })
export class PipelineService {
  private http = inject(HttpClient);

  readonly pipeline = signal<PipelineResponse | null>(null);
  readonly config = signal<PipelineConfig>({ ...DEFAULT_CONFIG });
  readonly savedConfig = signal<PipelineConfig>({ ...DEFAULT_CONFIG });
  readonly documents = signal<DocumentInfo[]>([]);
  readonly models = signal<ModelSelection | null>(null);
  readonly chunkPreview = signal<ChunkPreviewResponse | null>(null);
  readonly isSaving = signal(false);
  readonly isLoading = signal(false);
  readonly saveError = signal<string | null>(null);

  readonly hasUnsavedChanges = computed(() =>
    JSON.stringify(this.config()) !== JSON.stringify(this.savedConfig())
  );

  async loadPipeline(projectId: string): Promise<void> {
    this.isLoading.set(true);
    try {
      const pipelines = await firstValueFrom(
        this.http.get<PipelineResponse[]>('/api/projects/' + projectId + '/pipelines')
      );
      if (pipelines.length > 0) {
        const p = pipelines[0];
        this.pipeline.set(p);
        const merged = { ...DEFAULT_CONFIG, ...p.config };
        this.config.set(merged);
        this.savedConfig.set(merged);
      }
      try {
        const docs = await firstValueFrom(
          this.http.get<DocumentInfo[]>('/api/projects/' + projectId + '/documents')
        );
        this.documents.set(docs);
      } catch { this.documents.set([]); }
      try {
        const m = await firstValueFrom(this.http.get<ModelSelection>('/api/models'));
        this.models.set(m);
      } catch { /* ignore */ }
    } finally {
      this.isLoading.set(false);
    }
  }

  async savePipeline(projectId: string): Promise<void> {
    const p = this.pipeline();
    if (!p) return;
    this.isSaving.set(true);
    this.saveError.set(null);
    try {
      const updated = await firstValueFrom(
        this.http.put<PipelineResponse>(
          '/api/projects/' + projectId + '/pipelines/' + p.id,
          { name: p.name, description: p.description, config: this.config() }
        )
      );
      this.pipeline.set(updated);
      const merged = { ...DEFAULT_CONFIG, ...updated.config };
      this.savedConfig.set(merged);
      this.config.set(merged);
    } catch (err: any) {
      this.saveError.set(err?.error?.error ?? 'Failed to save pipeline');
      throw err;
    } finally {
      this.isSaving.set(false);
    }
  }

  resetDefaults(): void {
    this.config.set({ ...DEFAULT_CONFIG });
  }

  updateConfig(partial: Partial<PipelineConfig>): void {
    this.config.update(c => ({ ...c, ...partial }));
  }

  async loadChunkPreview(projectId: string, documentId: string): Promise<void> {
    try {
      const cfg = this.config();
      const preview = await firstValueFrom(
        this.http.get<ChunkPreviewResponse>(
          '/api/projects/' + projectId + '/documents/' + documentId + '/chunks',
          { params: { chunkSize: cfg.chunkSize.toString(), chunkOverlap: cfg.chunkOverlap.toString(), page: '1', pageSize: '3' } }
        )
      );
      this.chunkPreview.set(preview);
    } catch { this.chunkPreview.set(null); }
  }
}
