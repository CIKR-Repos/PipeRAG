import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { PipelineService } from './pipeline.service';

describe('PipelineService', () => {
  let service: PipelineService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PipelineService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PipelineService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have 5 default blocks', () => {
    expect(service.blocks().length).toBe(5);
  });

  it('should start on source step', () => {
    expect(service.currentStep()).toBe('source');
  });

  it('should navigate steps forward', () => {
    service.nextStep();
    expect(service.currentStep()).toBe('chunking');
    service.nextStep();
    expect(service.currentStep()).toBe('embedding');
  });

  it('should navigate steps backward', () => {
    service.goToStep('generation');
    service.prevStep();
    expect(service.currentStep()).toBe('retrieval');
  });

  it('should not go before first step', () => {
    service.prevStep();
    expect(service.currentStep()).toBe('source');
  });

  it('should not go past last step', () => {
    service.goToStep('review');
    service.nextStep();
    expect(service.currentStep()).toBe('review');
  });

  it('should update block config', () => {
    const chunkingId = service.blocks().find(b => b.type === 'chunking')!.id;
    service.updateBlockConfig(chunkingId, 'chunkSize', 1024);
    const chunking = service.blocks().find(b => b.type === 'chunking')!;
    expect(chunking.config['chunkSize']).toBe(1024);
  });

  it('should mark dirty on config change', () => {
    expect(service.hasUnsavedChanges()).toBeFalsy();
    const chunkingId = service.blocks().find(b => b.type === 'chunking')!.id;
    service.updateBlockConfig(chunkingId, 'chunkSize', 1024);
    expect(service.hasUnsavedChanges()).toBeTruthy();
  });

  it('should select block', () => {
    const embeddingId = service.blocks().find(b => b.type === 'embedding')!.id;
    service.selectBlock(embeddingId);
    expect(service.selectedBlockId()).toBe(embeddingId);
    expect(service.selectedBlock()?.type).toBe('embedding');
  });

  it('should move blocks', () => {
    service.moveBlock(0, 2);
    expect(service.blocks()[0].type).toBe('chunking');
    expect(service.blocks()[2].type).toBe('source');
  });

  it('should calculate progress percent', () => {
    // source is step 0, 1/6 ~ 17%
    expect(service.progressPercent()).toBe(17);
    service.goToStep('review');
    expect(service.progressPercent()).toBe(100);
  });

  it('should reset to defaults', () => {
    const chunkingId = service.blocks().find(b => b.type === 'chunking')!.id;
    service.updateBlockConfig(chunkingId, 'chunkSize', 2048);
    service.resetDefaults();
    expect(service.blocks().find(b => b.type === 'chunking')!.config['chunkSize']).toBe(512);
    expect(service.dirty()).toBeFalsy();
  });

  it('should generate chunk previews reactively', () => {
    const previews = service.chunkPreviews();
    expect(previews.length).toBeGreaterThan(0);
    expect(previews[0].tokens).toBeGreaterThan(0);
  });

  it('should compute config from blocks', () => {
    const cfg = service.config();
    expect(cfg.chunkSize).toBe(512);
    expect(cfg.topK).toBe(5);
  });

  it('should load pipeline from API', async () => {
    const promise = service.loadPipeline('proj-1');
    const pipeReq = httpMock.expectOne('/api/projects/proj-1/pipelines');
    pipeReq.flush([{
      id: 'pipe-1', projectId: 'proj-1', name: 'Test', description: null,
      config: { chunkSize: 256, chunkOverlap: 30, embeddingModel: 'text-embedding-3-large', retrievalStrategy: 'hybrid', topK: 10, scoreThreshold: 0.8 },
      status: 'idle', createdAt: '2026-01-01T00:00:00Z', updatedAt: null,
    }]);
    // loadDocuments call
    const docReq = httpMock.expectOne('/api/projects/proj-1/documents');
    docReq.flush([]);
    await promise;
    expect(service.pipelineId()).toBe('pipe-1');
    expect(service.blocks().find(b => b.type === 'chunking')!.config['chunkSize']).toBe(256);
  });

  it('should save pipeline via PUT', async () => {
    // Load first
    const loadPromise = service.loadPipeline('proj-1');
    httpMock.expectOne('/api/projects/proj-1/pipelines').flush([{
      id: 'pipe-1', projectId: 'proj-1', name: 'Test', description: null,
      config: { chunkSize: 512, chunkOverlap: 50, embeddingModel: 'text-embedding-3-small', retrievalStrategy: 'semantic', topK: 5, scoreThreshold: 0.7 },
      status: 'idle', createdAt: '2026-01-01T00:00:00Z', updatedAt: null,
    }]);
    httpMock.expectOne('/api/projects/proj-1/documents').flush([]);
    await loadPromise;

    const savePromise = service.savePipeline('proj-1');
    const saveReq = httpMock.expectOne('/api/projects/proj-1/pipelines/pipe-1');
    expect(saveReq.request.method).toBe('PUT');
    saveReq.flush({});
    await savePromise;
    expect(service.dirty()).toBeFalsy();
  });

  it('should handle save error', async () => {
    const loadPromise = service.loadPipeline('proj-1');
    httpMock.expectOne('/api/projects/proj-1/pipelines').flush([{
      id: 'pipe-1', projectId: 'proj-1', name: 'Test', description: null,
      config: { chunkSize: 512, chunkOverlap: 50, embeddingModel: 'text-embedding-3-small', retrievalStrategy: 'semantic', topK: 5, scoreThreshold: 0.7 },
      status: 'idle', createdAt: '2026-01-01T00:00:00Z', updatedAt: null,
    }]);
    httpMock.expectOne('/api/projects/proj-1/documents').flush([]);
    await loadPromise;

    const savePromise = service.savePipeline('proj-1').catch(() => {});
    httpMock.expectOne('/api/projects/proj-1/pipelines/pipe-1').flush(
      { error: 'Bad request' }, { status: 400, statusText: 'Bad Request' }
    );
    await savePromise;
    expect(service.saveError()).toBeTruthy();
  });

  it('should run pipeline and set status', async () => {
    service.pipelineId.set('pipe-1');
    const runPromise = service.runPipeline('proj-1');
    expect(service.pipelineStatus()).toBe('running');
    const runReq = httpMock.expectOne('/api/projects/proj-1/pipelines/pipe-1/run');
    expect(runReq.request.method).toBe('POST');
    runReq.flush({});
    await runPromise;
    expect(service.pipelineStatus()).toBe('completed');
    expect(service.isRunning()).toBeFalsy();
  });

  it('should handle run pipeline failure', async () => {
    service.pipelineId.set('pipe-1');
    const runPromise = service.runPipeline('proj-1');
    httpMock.expectOne('/api/projects/proj-1/pipelines/pipe-1/run').flush({}, { status: 500, statusText: 'Error' });
    await runPromise;
    expect(service.pipelineStatus()).toBe('failed');
  });

  it('should go to step directly', () => {
    service.goToStep('retrieval');
    expect(service.currentStep()).toBe('retrieval');
    expect(service.currentStepIndex()).toBe(3);
  });

  it('should load documents', async () => {
    const promise = service.loadDocuments('proj-1');
    httpMock.expectOne('/api/projects/proj-1/documents').flush([
      { id: 'd1', fileName: 'test.pdf', fileType: 'application/pdf', fileSize: 1024, status: 'Processed' },
    ]);
    await promise;
    expect(service.documents().length).toBe(1);
    expect(service.documents()[0].fileName).toBe('test.pdf');
  });
});
