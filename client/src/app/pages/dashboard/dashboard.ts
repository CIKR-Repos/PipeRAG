import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { NavbarComponent } from '../../shared/components/navbar/navbar';

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
  styleUrl: './dashboard.scss',
})
export class DashboardComponent implements OnInit {
  private http = inject(HttpClient);
  private router = inject(Router);

  projects = signal<ProjectSummary[]>([]);
  loading = signal(true);
  showCreate = signal(false);
  newName = '';
  newDesc = '';
  creating = signal(false);

  ngOnInit() {
    this.loadProjects();
  }

  async loadProjects() {
    this.loading.set(true);
    this.http.get<ProjectSummary[]>('/api/projects').subscribe({
      next: (data) => {
        this.projects.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openCreate() {
    this.showCreate.set(true);
    this.newName = '';
    this.newDesc = '';
  }

  closeCreate() {
    this.showCreate.set(false);
  }

  createProject() {
    if (!this.newName.trim()) return;
    this.creating.set(true);
    this.http.post<ProjectSummary>('/api/projects', {
      name: this.newName.trim(),
      description: this.newDesc.trim() || null,
    }).subscribe({
      next: (p) => {
        this.creating.set(false);
        this.showCreate.set(false);
        this.router.navigate(['/projects', p.id, 'chat']);
      },
      error: () => this.creating.set(false),
    });
  }

  timeAgo(dateStr: string | null): string {
    if (!dateStr) return 'Never';
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'Just now';
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    const days = Math.floor(hrs / 24);
    return `${days}d ago`;
  }
}
