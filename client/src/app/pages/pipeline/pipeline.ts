import { Component } from '@angular/core';
import { NavbarComponent } from '../../shared/components/navbar/navbar';

@Component({
  selector: 'app-pipeline',
  standalone: true,
  imports: [NavbarComponent],
  template: `
    <app-navbar />
    <div class="text-center py-24 px-6 text-gray-500">
      <div class="text-5xl mb-4">🔧</div>
      <h2 class="text-2xl font-bold text-dark mb-2">Pipeline Builder</h2>
      <p>Visual pipeline configuration coming soon.</p>
    </div>
  `,
})
export class PipelineComponent {}
