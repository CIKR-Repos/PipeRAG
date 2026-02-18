import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface SubscriptionInfo {
  tier: 'Free' | 'Pro' | 'Enterprise';
  status: string;
  currentPeriodEnd: string | null;
  stripeCustomerId: string | null;
}

export interface UsageInfo {
  queriesUsed: number;
  queriesLimit: number;
  documentsUsed: number;
  documentsLimit: number;
  projectsUsed: number;
  projectsLimit: number;
  storageBytesUsed: number;
  storageBytesLimit: number;
  tier: 'Free' | 'Pro' | 'Enterprise';
}

@Injectable({ providedIn: 'root' })
export class BillingService {
  private http = inject(HttpClient);

  readonly subscription = signal<SubscriptionInfo | null>(null);
  readonly usage = signal<UsageInfo | null>(null);
  readonly loading = signal(false);

  async loadSubscription(): Promise<void> {
    const sub = await firstValueFrom(this.http.get<SubscriptionInfo>('/api/billing/subscription'));
    this.subscription.set(sub);
  }

  async loadUsage(): Promise<void> {
    const usage = await firstValueFrom(this.http.get<UsageInfo>('/api/billing/usage'));
    this.usage.set(usage);
  }

  async createCheckoutSession(tier: string): Promise<string> {
    const res = await firstValueFrom(this.http.post<{ url: string }>('/api/billing/create-checkout-session', {
      tier,
      successUrl: `${window.location.origin}/billing?success=true`,
      cancelUrl: `${window.location.origin}/billing?cancelled=true`,
    }));
    return res.url;
  }

  async createPortalSession(): Promise<string> {
    const res = await firstValueFrom(this.http.post<{ url: string }>('/api/billing/create-portal-session', {
      returnUrl: `${window.location.origin}/billing`,
    }));
    return res.url;
  }
}
