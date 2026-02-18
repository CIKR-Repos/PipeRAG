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

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('projectId') ?? '';
    if (this.projectId) {
      this.svc.loadPipeline(this.projectId);
    }
  }

  onDrop(event: CdkDragDrop<PipelineBlock[]>): void {
    this.svc.moveBlock(event.previousIndex, event.currentIndex);
  }

  save(): void {
    if (this.projectId) {
      this.svc.savePipeline(this.projectId);
    }
  }

  onConfigChange(blockId: string, key: string, event: Event): void {
    const el = event.target as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
    this.svc.updateBlockConfig(blockId, key, el.value);
  }

  onConfigChangeNum(blockId: string, key: string, event: Event): void {
    const el = event.target as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
    const parsed = parseInt(el.value, 10);
    if (!isNaN(parsed)) this.svc.updateBlockConfig(blockId, key, parsed);
  }

  onConfigChangeFloat(blockId: string, key: string, event: Event): void {
    const el = event.target as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
    const parsed = parseFloat(el.value);
    if (!isNaN(parsed)) this.svc.updateBlockConfig(blockId, key, parsed);
  }

  onConfigChangeBool(blockId: string, key: string, event: Event): void {
    const el = event.target as HTMLInputElement;
    this.svc.updateBlockConfig(blockId, key, el.checked);
  }

  blockClass(block: PipelineBlock): string {
    const selected = this.svc.selectedBlockId() === block.id;
    const colors: Record<BlockType, string> = {
      source: selected ? 'border-emerald-500/50 bg-emerald-500/10' : 'border-border bg-surface-2',
      chunking: selected ? 'border-amber-500/50 bg-amber-500/10' : 'border-border bg-surface-2',
      embedding: selected ? 'border-purple-500/50 bg-purple-500/10' : 'border-border bg-surface-2',
      retrieval: selected ? 'border-blue-500/50 bg-blue-500/10' : 'border-border bg-surface-2',
      generation: selected ? 'border-rose-500/50 bg-rose-500/10' : 'border-border bg-surface-2',
    };
    return colors[block.type];
  }

  blockIconBg(block: PipelineBlock): string {
    const colors: Record<BlockType, string> = {
      source: 'bg-emerald-500/15', chunking: 'bg-amber-500/15', embedding: 'bg-purple-500/15',
      retrieval: 'bg-blue-500/15', generation: 'bg-rose-500/15',
    };
    return colors[block.type];
  }

  blockTypeBadge(block: PipelineBlock): string {
    const colors: Record<BlockType, string> = {
      source: 'text-emerald-400', chunking: 'text-amber-400', embedding: 'text-purple-400',
      retrieval: 'text-blue-400', generation: 'text-rose-400',
    };
    return colors[block.type];
  }

  blockSummary(block: PipelineBlock): string {
    const c = block.config;
    switch (block.type) {
      case 'source': return c['sourceType'] + ' · ' + c['fileTypes'];
      case 'chunking': return c['strategy'] + ' · ' + c['chunkSize'] + ' tokens · ' + c['chunkOverlap'] + ' overlap';
      case 'embedding': return c['model'] + ' · ' + c['dimensions'] + 'd';
      case 'retrieval': return c['strategy'] + ' · top ' + c['topK'] + ' · threshold ' + c['scoreThreshold'];
      case 'generation': return c['model'] + ' · temp ' + c['temperature'];
      default: return '';
    }
  }
}
