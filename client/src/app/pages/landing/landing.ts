import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './landing.html',
})
export class LandingComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  features = [
    { icon: '🪄', title: 'Auto-Pipeline', desc: 'Upload any document and we automatically build the optimal chunking, embedding, and retrieval pipeline.' },
    { icon: '👁️', title: 'Chunk Preview', desc: 'See exactly how your documents are split, embedded, and indexed. Full transparency into your RAG pipeline.' },
    { icon: '🚀', title: 'One-Click Deploy', desc: 'Deploy your chatbot instantly with a shareable link. No infrastructure to manage.' },
  ];

  steps = [
    { num: 1, title: 'Upload', desc: 'Drop your PDFs, docs, or text files' },
    { num: 2, title: 'Configure', desc: 'Customize chunking, embeddings & prompts' },
    { num: 3, title: 'Chat', desc: 'Ask questions and get accurate answers' },
  ];

  constructor() {
    if (this.auth.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }
  }
}
