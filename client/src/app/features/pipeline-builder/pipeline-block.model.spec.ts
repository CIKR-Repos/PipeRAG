import { createDefaultBlocks, BlockType } from './pipeline-block.model';

describe('PipelineBlock Model', () => {
  describe('createDefaultBlocks', () => {
    it('should return 5 blocks', () => {
      expect(createDefaultBlocks().length).toBe(5);
    });

    it('should have correct order: source → chunking → embedding → retrieval → generation', () => {
      const types = createDefaultBlocks().map(b => b.type);
      expect(types).toEqual(['source', 'chunking', 'embedding', 'retrieval', 'generation']);
    });

    it('should have unique IDs', () => {
      const ids = createDefaultBlocks().map(b => b.id);
      expect(new Set(ids).size).toBe(5);
    });

    it('should have default config values for chunking', () => {
      const chunking = createDefaultBlocks().find(b => b.type === 'chunking')!;
      expect(chunking.config['chunkSize']).toBe(512);
      expect(chunking.config['chunkOverlap']).toBe(50);
      expect(chunking.config['strategy']).toBe('recursive');
    });

    it('should have default config values for embedding', () => {
      const embedding = createDefaultBlocks().find(b => b.type === 'embedding')!;
      expect(embedding.config['model']).toBe('text-embedding-3-small');
      expect(embedding.config['dimensions']).toBe(1536);
    });

    it('should have default config values for generation', () => {
      const gen = createDefaultBlocks().find(b => b.type === 'generation')!;
      expect(gen.config['model']).toBe('gpt-4o-mini');
      expect(gen.config['temperature']).toBe(0.7);
      expect(gen.config['maxTokens']).toBe(2048);
    });

    it('should return independent copies each call', () => {
      const a = createDefaultBlocks();
      const b = createDefaultBlocks();
      a[0].config['test'] = 'modified';
      expect(b[0].config['test']).toBeUndefined();
    });

    it('should have icons for all blocks', () => {
      for (const block of createDefaultBlocks()) {
        expect(block.icon).toBeTruthy();
      }
    });

    it('should have labels for all blocks', () => {
      for (const block of createDefaultBlocks()) {
        expect(block.label).toBeTruthy();
      }
    });
  });
});
