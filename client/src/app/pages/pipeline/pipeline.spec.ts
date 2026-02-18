import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute } from '@angular/router';
import { PipelineComponent } from './pipeline';
import { PipelineService } from '../../features/pipeline-builder/pipeline.service';

describe('PipelineComponent', () => {
  let component: PipelineComponent;
  let fixture: ComponentFixture<PipelineComponent>;
  let svc: PipelineService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PipelineComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'test-project-id' } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PipelineComponent);
    component = fixture.componentInstance;
    svc = TestBed.inject(PipelineService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should return correct block classes for selected block', () => {
    const block = svc.blocks()[0]; // source
    svc.selectBlock(block.id);
    expect(component.blockClass(block)).toContain('border-emerald-500');
  });

  it('should return correct block classes for unselected block', () => {
    svc.selectBlock('nonexistent');
    const block = svc.blocks()[0];
    expect(component.blockClass(block)).toContain('border-gray-200');
  });

  it('should return block icon background', () => {
    const block = svc.blocks().find(b => b.type === 'embedding')!;
    expect(component.blockIconBg(block)).toBe('bg-purple-100');
  });

  it('should return block type badge color', () => {
    const block = svc.blocks().find(b => b.type === 'retrieval')!;
    expect(component.blockTypeBadge(block)).toBe('text-blue-700');
  });

  it('should return block summary for chunking', () => {
    const block = svc.blocks().find(b => b.type === 'chunking')!;
    const summary = component.blockSummary(block);
    expect(summary).toContain('recursive');
    expect(summary).toContain('512');
  });

  it('should return block summary for generation', () => {
    const block = svc.blocks().find(b => b.type === 'generation')!;
    expect(component.blockSummary(block)).toContain('gpt-4o-mini');
  });

  it('should return block summary for embedding', () => {
    const block = svc.blocks().find(b => b.type === 'embedding')!;
    expect(component.blockSummary(block)).toContain('1536d');
  });
});
