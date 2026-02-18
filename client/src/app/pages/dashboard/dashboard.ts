import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { NavbarComponent } from '../../shared/components/navbar/navbar';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

interface ProjectSummary {
  id: string;
  name: string;
  description: string | null;
  documentCount: number;
  createdAt: string;
  updatedAt: string | null;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [NavbarComponent, RouterLink, FormsModule],
  templateUrl: './dashboard.html',
  styles: [`
    @keyframes stagger-in {
      from { opacity: 0; transform: translateY(16px); }
      to { opacity: 1; transform: translateY(0); }
    }
    .stagger-item {
      animation: stagger-in 0.5s cubic-bezier(0.16, 1, 0.3, 1) both;
    }
  `]
})
export class DashboardComponent implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  projects = signal<ProjectSummary[]>([]);
  totalDocs = computed(() => this.projects().reduce((sum, p) => sum + p.documentCount, 0));
  loading = signal(true);
  showCreate = signal(false);
  newName = '';
  newDesc = '';
  creating = signal(false);

  userName = computed(() => {
    const name = this.auth.user()?.displayName ?? '';
    return name.split(' ')[0] || 'there';
  });

  greeting = computed(() => {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  });

  skeletonCards = Array.from({ length: 6 });

  ngOnInit() {
    this.loadProjects();
  }

  loadProjects() {
    this.loading.set(true);
    this.http.get<ProjectSummary[]>('/api/projects').subscribe({
      next: (data) => { this.projects.set(data); this.loading.set(false); },
      error: () => { this.loading.set(false); this.toast.error('Failed to load projects'); },
    });
  }

  openCreate() { this.showCreate.set(true); this.newName = ''; this.newDesc = ''; }
  closeCreate() { this.showCreate.set(false); }

  createProject() {
    if (!this.newName.trim()) return;
    this.creating.set(true);
    this.http.post<ProjectSummary>('/api/projects', {
      name: this.newName.trim(),
      description: this.newDesc.trim() || null,
    }).subscribe({
      next: (p) => { this.creating.set(false); this.showCreate.set(false); this.router.navigate(['/projects', p.id, 'chat']); },
      error: () => { this.creating.set(false); this.toast.error('Failed to create project'); },
    });
  }

  staggerDelay(index: number): string {
    return (index * 75) + 'ms';
  }

  timeAgo(dateStr: string | null): string {
    if (!dateStr) return 'Never';
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'Just now';
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    return `${Math.floor(hrs / 24)}d ago`;
  }
}
