import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserProfile;
}

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  tier: 'Free' | 'Pro' | 'Enterprise';
  isActive: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly _user = signal<UserProfile | null>(null);
  private readonly _token = signal<string | null>(null);

  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => !!this._token());
  readonly userTier = computed(() => this._user()?.tier ?? 'Free');

  constructor() {
    this.loadFromStorage();
  }

  async login(email: string, password: string): Promise<void> {
    const res = await this.http
      .post<AuthResponse>('/api/auth/login', { email, password })
      .toPromise();
    if (res) this.handleAuth(res);
  }

  async register(email: string, password: string, displayName: string): Promise<void> {
    const res = await this.http
      .post<AuthResponse>('/api/auth/register', { email, password, displayName })
      .toPromise();
    if (res) this.handleAuth(res);
  }

  async refresh(): Promise<string | null> {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) return null;
    try {
      const res = await this.http
        .post<AuthResponse>('/api/auth/refresh', { refreshToken })
        .toPromise();
      if (res) {
        this.handleAuth(res);
        return res.accessToken;
      }
    } catch {
      this.logout();
    }
    return null;
  }

  logout(): void {
    this._user.set(null);
    this._token.set(null);
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return this._token();
  }

  private handleAuth(res: AuthResponse): void {
    this._token.set(res.accessToken);
    this._user.set(res.user);
    localStorage.setItem('accessToken', res.accessToken);
    localStorage.setItem('refreshToken', res.refreshToken);
    localStorage.setItem('user', JSON.stringify(res.user));
  }

  private loadFromStorage(): void {
    const token = localStorage.getItem('accessToken');
    const userJson = localStorage.getItem('user');
    if (token && userJson) {
      this._token.set(token);
      this._user.set(JSON.parse(userJson));
    }
  }
}
