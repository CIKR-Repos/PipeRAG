import { Component } from '@angular/core';
import { NavbarComponent } from '../../shared/components/navbar/navbar';

@Component({
  selector: 'app-pipeline',
  standalone: true,
  imports: [NavbarComponent],
  template: `
    <app-navbar />
    <div class="pipeline-placeholder">
      <div class="icon">🔧</div>
      <h2>Pipeline Builder</h2>
      <p>Visual pipeline configuration coming soon.</p>
    </div>
  `,
  styles: [`
    .pipeline-placeholder {
      text-align: center;
      padding: 6rem 2rem;
      color: #6c757d;
    }
    .icon { font-size: 3rem; margin-bottom: 1rem; }
    h2 { color: #1a1a2e; font-size: 1.5rem; margin-bottom: 0.5rem; }
  `],
})
export class PipelineComponent {}
