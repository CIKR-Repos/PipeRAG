export type BlockType = 'source' | 'chunking' | 'embedding' | 'retrieval' | 'generation';

export interface PipelineBlock {
  id: string;
  type: BlockType;
  label: string;
  icon: string;
  config: Record<string, unknown>;
}

export interface ChunkPreview {
  index: number;
  text: string;
  tokens: number;
}

export function createDefaultBlocks(): PipelineBlock[] {
  return [
    { id: crypto.randomUUID(), type: 'source', label: 'Document Source', icon: '📄', config: { sourceType: 'upload', fileTypes: '.pdf,.txt,.md,.docx' } },
    { id: crypto.randomUUID(), type: 'chunking', label: 'Text Chunking', icon: '✂️', config: { strategy: 'recursive', chunkSize: 512, chunkOverlap: 50 } },
    { id: crypto.randomUUID(), type: 'embedding', label: 'Embeddings', icon: '🧬', config: { model: 'text-embedding-3-small', dimensions: 1536, batchSize: 100 } },
    { id: crypto.randomUUID(), type: 'retrieval', label: 'Retrieval', icon: '🔍', config: { strategy: 'semantic', topK: 5, scoreThreshold: 0.7, reranking: false } },
    { id: crypto.randomUUID(), type: 'generation', label: 'Generation', icon: '🤖', config: { model: 'gpt-4o-mini', temperature: 0.7, maxTokens: 2048, systemPrompt: '' } },
  ];
}
