import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { PipelineBlock, ChunkPreview, createDefaultBlocks } from './pipeline-block.model';

export interface PipelineDto {
  id: string;
  projectId: string;
  name: string;
  description: string | null;
  config: {
    chunkSize: number;
    chunkOverlap: number;
    embeddingModel: string;
    retrievalStrategy: string;
    topK: number;
    scoreThreshold: number;
  };
  status: string;
  createdAt: string;
  updatedAt: string | null;
}

const SAMPLE_TEXT =
  'Retrieval-Augmented Generation (RAG) is a technique that combines information retrieval with text generation. ' +
  'It first retrieves relevant documents from a knowledge base using semantic search, then feeds those documents ' +
  'as context to a large language model (LLM) to generate accurate, grounded responses. This approach reduces ' +
  'hallucinations and allows the model to access up-to-date information without retraining. RAG pipelines typically ' +
  'consist of several stages: document ingestion, text chunking, embedding generation, vector storage, retrieval, ' +
  'and finally generation. Each stage can be configured independently to optimize the overall pipeline performance ' +
  'for specific use cases.';

@Injectable({ providedIn: 'root' })
export class PipelineService {
  private http = inject(HttpClient);

  readonly blocks = signal<PipelineBlock[]>(createDefaultBlocks());
  readonly selectedBlockId = signal<string | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly dirty = signal(false);
  readonly pipelineId = signal<string | null>(null);

  readonly selectedBlock = computed(() => {
    const id = this.selectedBlockId();
    return id ? this.blocks().find((b: PipelineBlock) => b.id === id) ?? null : null;
  });

  readonly chunkPreviews = computed<ChunkPreview[]>(() => {
    const chunkingBlock = this.blocks().find((b: PipelineBlock) => b.type === 'chunking');
    if (!chunkingBlock) return [];
    const size: number = chunkingBlock.config['chunkSize'] ?? 512;
    const overlap: number = chunkingBlock.config['chunkOverlap'] ?? 50;
    return this.generateChunkPreviews(SAMPLE_TEXT, size, overlap);
  });

  readonly config = computed(() => {
    const chunking = this.blocks().find((b: PipelineBlock) => b.type === 'chunking');
    const retrieval = this.blocks().find((b: PipelineBlock) => b.type === 'retrieval');
    const generation = this.blocks().find((b: PipelineBlock) => b.type === 'generation');
    return {
      chunkSize: (chunking?.config['chunkSize'] as number) ?? 512,
      chunkOverlap: (chunking?.config['chunkOverlap'] as number) ?? 50,
      retrievalStrategy: (retrieval?.config['strategy'] as string) ?? 'semantic',
      topK: (retrieval?.config['topK'] as number) ?? 5,
      scoreThreshold: (retrieval?.config['scoreThreshold'] as number) ?? 0.7,
      temperature: (generation?.config['temperature'] as number) ?? 0.7,
    };
  });

  readonly hasUnsavedChanges = this.dirty;

  selectBlock(id: string): void {
    this.selectedBlockId.set(id);
  }

  updateBlockConfig(blockId: string, key: string, value: unknown): void {
    this.blocks.update((blocks: PipelineBlock[]) =>
      blocks.map((b: PipelineBlock) =>
        b.id === blockId ? { ...b, config: { ...b.config, [key]: value } } : b,
      ),
    );
    this.dirty.set(true);
  }

  updateConfig(partial: Partial<{ chunkSize: number; chunkOverlap: number; temperature: number }>): void {
    if (partial.chunkSize !== undefined || partial.chunkOverlap !== undefined) {
      const chunking = this.blocks().find((b: PipelineBlock) => b.type === 'chunking');
      if (chunking) {
        if (partial.chunkSize !== undefined) this.updateBlockConfig(chunking.id, 'chunkSize', partial.chunkSize);
        if (partial.chunkOverlap !== undefined) this.updateBlockConfig(chunking.id, 'chunkOverlap', partial.chunkOverlap);
      }
    }
    if (partial.temperature !== undefined) {
      const gen = this.blocks().find((b: PipelineBlock) => b.type === 'generation');
      if (gen) this.updateBlockConfig(gen.id, 'temperature', partial.temperature);
    }
    this.dirty.set(true);
  }

  resetDefaults(): void {
    this.blocks.set(createDefaultBlocks());
    this.dirty.set(false);
    this.selectedBlockId.set(null);
  }

  moveBlock(fromIndex: number, toIndex: number): void {
    this.blocks.update((blocks: PipelineBlock[]) => {
      const arr = [...blocks];
      const [removed] = arr.splice(fromIndex, 1);
      arr.splice(toIndex, 0, removed);
      return arr;
    });
    this.dirty.set(true);
  }

  async loadPipeline(projectId: string): Promise<void> {
    this.loading.set(true);
    try {
      const pipelines = await firstValueFrom(
        this.http.get<PipelineDto[]>('/api/projects/' + projectId + '/pipelines'),
      );
      if (pipelines.length > 0) {
        const p = pipelines[0];
        this.pipelineId.set(p.id);
        const blocks = createDefaultBlocks();
        const chunking = blocks.find((b: PipelineBlock) => b.type === 'chunking')!;
        chunking.config['chunkSize'] = p.config.chunkSize;
        chunking.config['chunkOverlap'] = p.config.chunkOverlap;
        const embedding = blocks.find((b: PipelineBlock) => b.type === 'embedding')!;
        embedding.config['model'] = p.config.embeddingModel;
        const retrieval = blocks.find((b: PipelineBlock) => b.type === 'retrieval')!;
        retrieval.config['strategy'] = p.config.retrievalStrategy;
        retrieval.config['topK'] = p.config.topK;
        retrieval.config['scoreThreshold'] = p.config.scoreThreshold;
        this.blocks.set(blocks);
      }
    } finally {
      this.loading.set(false);
      this.dirty.set(false);
    }
  }

  async savePipeline(projectId: string): Promise<void> {
    this.saving.set(true);
    try {
      const cfg = this.config();
      const body = {
        name: 'Default Pipeline',
        description: 'Visual pipeline configuration',
        config: {
          chunkSize: cfg.chunkSize,
          chunkOverlap: cfg.chunkOverlap,
          embeddingModel:
            (this.blocks().find((b: PipelineBlock) => b.type === 'embedding')?.config['model'] as string) ??
            'text-embedding-3-small',
          retrievalStrategy: cfg.retrievalStrategy,
          topK: cfg.topK,
          scoreThreshold: cfg.scoreThreshold,
        },
      };

      const pid = this.pipelineId();
      if (pid) {
        await firstValueFrom(this.http.put('/api/projects/' + projectId + '/pipelines/' + pid, body));
      } else {
        const result = await firstValueFrom(
          this.http.post<PipelineDto>('/api/projects/' + projectId + '/pipelines', body),
        );
        this.pipelineId.set(result.id);
      }
      this.dirty.set(false);
    } finally {
      this.saving.set(false);
    }
  }

  private generateChunkPreviews(text: string, size: number, overlap: number): ChunkPreview[] {
    const charSize = Math.max(size, 50);
    const charOverlap = Math.min(overlap, charSize - 10);
    const step = Math.max(charSize - charOverlap, 1);
    const chunks: ChunkPreview[] = [];
    for (let i = 0; i < text.length && chunks.length < 5; i += step) {
      const slice = text.slice(i, i + charSize);
      chunks.push({ index: chunks.length, text: slice, tokens: Math.round(slice.length / 4) });
    }
    return chunks;
  }
}
