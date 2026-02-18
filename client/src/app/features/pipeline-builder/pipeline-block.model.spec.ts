import { defaultBlocks, getBlockFields, BLOCK_ICONS, BLOCK_LABELS, BlockType } from './pipeline-block.model';

describe('PipelineBlock Model', () => {
  describe('defaultBlocks', () => {
    it('should return 5 blocks', () => {
      expect(defaultBlocks().length).toBe(5);
    });

    it('should have correct order: source → chunking → embedding → retrieval → generation', () => {
      const types = defaultBlocks().map(b => b.type);
      expect(types).toEqual(['source', 'chunking', 'embedding', 'retrieval', 'generation']);
    });

    it('should have unique IDs', () => {
      const ids = defaultBlocks().map(b => b.id);
      expect(new Set(ids).size).toBe(5);
    });

    it('should have default config values for chunking', () => {
      const chunking = defaultBlocks().find(b => b.type === 'chunking')!;
      expect(chunking.config['chunkSize']).toBe(512);
      expect(chunking.config['chunkOverlap']).toBe(50);
      expect(chunking.config['strategy']).toBe('recursive');
    });

    it('should have default config values for embedding', () => {
      const embedding = defaultBlocks().find(b => b.type === 'embedding')!;
      expect(embedding.config['model']).toBe('text-embedding-3-small');
      expect(embedding.config['dimensions']).toBe(1536);
    });

    it('should have default config values for generation', () => {
      const gen = defaultBlocks().find(b => b.type === 'generation')!;
      expect(gen.config['model']).toBe('gpt-4o-mini');
      expect(gen.config['temperature']).toBe(0.7);
      expect(gen.config['maxTokens']).toBe(2048);
    });

    it('should return independent copies each call', () => {
      const a = defaultBlocks();
      const b = defaultBlocks();
      a[0].config['test'] = 'modified';
      expect(b[0].config['test']).toBeUndefined();
    });
  });

  describe('BLOCK_ICONS', () => {
    it('should have icons for all 5 block types', () => {
      const types: BlockType[] = ['source', 'chunking', 'embedding', 'retrieval', 'generation'];
      for (const t of types) {
        expect(BLOCK_ICONS[t]).toBeTruthy();
      }
    });
  });

  describe('BLOCK_LABELS', () => {
    it('should have labels for all block types', () => {
      expect(BLOCK_LABELS['source']).toBe('Data Source');
      expect(BLOCK_LABELS['generation']).toBe('Generation');
    });
  });

  describe('getBlockFields', () => {
    it('should return fields for chunking block', () => {
      const fields = getBlockFields('chunking');
      expect(fields.length).toBe(3);
      expect(fields.map(f => f.key)).toEqual(['strategy', 'chunkSize', 'chunkOverlap']);
    });

    it('should return fields for embedding block', () => {
      const fields = getBlockFields('embedding');
      expect(fields.length).toBe(2);
      expect(fields[0].key).toBe('model');
    });

    it('should return fields for retrieval block', () => {
      const fields = getBlockFields('retrieval');
      expect(fields.find(f => f.key === 'topK')?.max).toBe(20);
    });

    it('should return fields for generation block', () => {
      const fields = getBlockFields('generation');
      const modelField = fields.find(f => f.key === 'model')!;
      expect(modelField.options!.length).toBeGreaterThanOrEqual(3);
    });

    it('should return fields for source block', () => {
      const fields = getBlockFields('source');
      expect(fields.length).toBe(1);
      expect(fields[0].key).toBe('sourceType');
    });
  });
});
