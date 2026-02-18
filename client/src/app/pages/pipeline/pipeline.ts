import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CdkDropList, CdkDrag, CdkDragDrop, CdkDragHandle, CdkDragPlaceholder } from '@angular/cdk/drag-drop';
import { PipelineService, PipelineBlock, BlockType } from './pipeline.service';

const BLOCK_COLORS: Record<BlockType, { border: string; bg: string; iconBg: string; badge: string }> = {
  source: { border: 'border-blue-300', bg: 'bg-blue-50', iconBg: 'bg-blue-100', badge: 'text-blue-700' },
  chunking: { border: 'border-amber-300', bg: 'bg-amber-50', iconBg: 'bg-amber-100', badge: 'text-amber-700' },
  embedding: { border: 'border-purple-300', bg: 'bg-purple-50', iconBg: 'bg-purple-100', badge: 'text-purple-700' },
  retrieval: { border: 'border-emerald-300', bg: 'bg-emerald-50', iconBg: 'bg-emerald-100', badge: 'text-emerald-700' },
  generation: { border: 'border-rose-300', bg: 'bg-rose-50', iconBg: 'bg-rose-100', badge: 'text-rose-700' },
};

@Component({
  selector: 'app-pipeline',
  standalone: true,
  imports: [CdkDropList, CdkDrag, CdkDragHandle, CdkDragPlaceholder],
  providers: [PipelineService],
  templateUrl: './pipeline.html',
})
export class PipelineComponent implements OnInit {
  private route = inject(ActivatedRoute);
  readonly svc: PipelineService = inject(PipelineService);
  private projectId = '';

  ngOnInit() {
    this.projectId = this.route.snapshot.paramMap.get('projectId') ?? '';
    if (this.projectId) {
      this.svc.loadPipeline(this.projectId);
    }
  }

  onDrop(event: CdkDragDrop<PipelineBlock[]>) {
    if (event.previousIndex !== event.currentIndex) {
      this.svc.reorderBlocks(event.previousIndex, event.currentIndex);
    }
  }

  save() {
    if (this.projectId) {
      this.svc.savePipeline(this.projectId);
    }
  }

  blockClass(block: PipelineBlock): string {
    const c = BLOCK_COLORS[block.type];
    const selected = this.svc.selectedBlockId() === block.id;
    return `${c.border} ${c.bg} ${selected ? 'ring-2 ring-indigo-500 shadow-md' : ''}`;
  }

  blockIconBg(block: PipelineBlock): string {
    return BLOCK_COLORS[block.type].iconBg;
  }

  blockTypeBadge(block: PipelineBlock): string {
    return BLOCK_COLORS[block.type].badge;
  }

  blockSummary(block: PipelineBlock): string {
    const c = block.config;
    switch (block.type) {
      case 'source': return `${c['sourceType']} · ${c['fileTypes']}`;
      case 'chunking': return `${c['strategy']} · ${c['chunkSize']} chars · ${c['chunkOverlap']} overlap`;
      case 'embedding': return `${c['model']} · ${c['dimensions']}d`;
      case 'retrieval': return `${c['strategy']} · top ${c['topK']} · threshold ${c['scoreThreshold']}`;
      case 'generation': return `${c['model']} · temp ${c['temperature']} · ${c['maxTokens']} tokens`;
      default: return '';
    }
  }

  onConfigChange(blockId: string, key: string, event: Event) {
    const el = event.target as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
    this.svc.updateBlockConfig(blockId, key, el.value);
  }

  onConfigChangeNum(blockId: string, key: string, event: Event) {
    const el = event.target as HTMLInputElement;
    this.svc.updateBlockConfig(blockId, key, parseInt(el.value, 10));
  }

  onConfigChangeFloat(blockId: string, key: string, event: Event) {
    const el = event.target as HTMLInputElement;
    this.svc.updateBlockConfig(blockId, key, parseFloat(el.value));
  }

  onConfigChangeBool(blockId: string, key: string, event: Event) {
    const el = event.target as HTMLInputElement;
    this.svc.updateBlockConfig(blockId, key, el.checked);
  }
}
