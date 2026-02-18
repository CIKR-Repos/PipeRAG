import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CdkDropList, CdkDrag, CdkDragDrop, CdkDragHandle, CdkDragPlaceholder } from '@angular/cdk/drag-drop';
import { NavbarComponent } from '../../shared/components/navbar/navbar';
import { PipelineService } from '../../features/pipeline-builder/pipeline.service';
import { PipelineBlock, BlockType } from '../../features/pipeline-builder/pipeline-block.model';

@Component({
  selector: 'app-pipeline',
  standalone: true,
  imports: [NavbarComponent, CdkDropList, CdkDrag, CdkDragHandle, CdkDragPlaceholder],
  templateUrl: './pipeline.html',
})
export class PipelineComponent implements OnInit {
  readonly svc = inject(PipelineService);
  private route = inject(ActivatedRoute);
  private projectId = '';

  ngOnInit() {
    this.projectId = this.route.snapshot.paramMap.get('projectId') ?? '';
    if (this.projectId) {
      this.svc.loadPipeline(this.projectId);
    }
  }

  onDrop(event: CdkDragDrop<PipelineBlock[]>) {
    this.svc.moveBlock(event.previousIndex, event.currentIndex);
  }

  save() {
    if (this.projectId) {
      this.svc.savePipeline(this.projectId);
    }
  }

  runPipeline() {
    if (this.projectId) {
      this.svc.runPipeline(this.projectId);
    }
  }

  blockClass(block: PipelineBlock): string {
    const selected = this.svc.selectedBlockId() === block.id;
    const colors: Record<BlockType, string> = {
      source: selected ? 'border-emerald-500 bg-emerald-50' : 'border-gray-200 bg-white',
      chunking: selected ? 'border-amber-500 bg-amber-50' : 'border-gray-200 bg-white',
      embedding: selected ? 'border-purple-500 bg-purple-50' : 'border-gray-200 bg-white',
      retrieval: selected ? 'border-blue-500 bg-blue-50' : 'border-gray-200 bg-white',
      generation: selected ? 'border-rose-500 bg-rose-50' : 'border-gray-200 bg-white',
    };
    return colors[block.type];
  }

  blockIconBg(block: PipelineBlock): string {
    const colors: Record<BlockType, string> = {
      source: 'bg-emerald-100',
      chunking: 'bg-amber-100',
      embedding: 'bg-purple-100',
      retrieval: 'bg-blue-100',
      generation: 'bg-rose-100',
    };
    return colors[block.type];
  }

  blockTypeBadge(block: PipelineBlock): string {
    const colors: Record<BlockType, string> = {
      source: 'text-emerald-700',
      chunking: 'text-amber-700',
      embedding: 'text-purple-700',
      retrieval: 'text-blue-700',
      generation: 'text-rose-700',
    };
    return colors[block.type];
  }

  blockSummary(block: PipelineBlock): string {
    switch (block.type) {
      case 'source': return `${block.config['sourceType']} · ${block.config['fileTypes']}`;
      case 'chunking': return `${block.config['strategy']} · ${block.config['chunkSize']} tokens · ${block.config['chunkOverlap']} overlap`;
      case 'embedding': return `${block.config['model']} · ${block.config['dimensions']}d`;
      case 'retrieval': return `${block.config['strategy']} · top ${block.config['topK']} · threshold ${block.config['scoreThreshold']}`;
      case 'generation': return `${block.config['model']} · temp ${block.config['temperature']}`;
      default: return '';
    }
  }

  onConfigChange(blockId: string, key: string, event: Event) {
    const el = event.target as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
    this.svc.updateBlockConfig(blockId, key, el.value);
  }

  onConfigChangeNum(blockId: string, key: string, event: Event) {
    const el = event.target as HTMLInputElement;
    const parsed = parseInt(el.value, 10);
    if (!isNaN(parsed)) {
      this.svc.updateBlockConfig(blockId, key, parsed);
    }
  }

  onConfigChangeFloat(blockId: string, key: string, event: Event) {
    const el = event.target as HTMLInputElement;
    const parsed = parseFloat(el.value);
    if (!isNaN(parsed)) {
      this.svc.updateBlockConfig(blockId, key, parsed);
    }
  }

  onConfigChangeBool(blockId: string, key: string, event: Event) {
    const el = event.target as HTMLInputElement;
    this.svc.updateBlockConfig(blockId, key, el.checked);
  }
}
