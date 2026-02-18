import { Component, inject, OnInit, computed, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NavbarComponent } from '../../shared/components/navbar/navbar';
import { BillingService, UsageInfo } from '../../core/services/billing.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [NavbarComponent, DatePipe],
  template: `
    <app-navbar />
    <main class="max-w-5xl mx-auto px-6 py-8">

      <!-- Header -->
      <div class="mb-8 animate-fade-in">
        <h1 class="text-2xl font-bold text-text-primary tracking-tight">Billing & Usage</h1>
        <p class="text-text-secondary text-sm mt-1">Manage your subscription and monitor resource usage</p>
      </div>

      <!-- Current Plan Card -->
      <section class="bg-surface-2 border border-border rounded-2xl overflow-hidden mb-8 animate-fade-in">
        <div class="h-1 bg-gradient-to-r from-accent via-purple-500 to-cyan-500"></div>
        <div class="p-6 flex items-center justify-between">
          <div>
            <p class="text-xs font-medium text-text-tertiary uppercase tracking-wider mb-1">Current Plan</p>
            <h2 class="text-2xl font-bold text-text-primary">{{ currentTier() }}</h2>
            @if (subscription()?.currentPeriodEnd) {
              <p class="text-sm text-text-muted mt-1.5 flex items-center gap-1.5">
                <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round"
                        d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                Renews {{ subscription()!.currentPeriodEnd | date:'mediumDate' }}
              </p>
            }
          </div>
          @if (currentTier() !== 'Free') {
            <button (click)="manageSubscription()"
                    class="px-5 py-2.5 bg-surface-3 hover:bg-surface-3/80 text-text-primary
                           border border-border rounded-xl text-sm font-medium cursor-pointer
                           transition-all duration-200 hover:border-border-subtle">
              Manage Subscription
            </button>
          }
        </div>
      </section>

      <!-- Usage Dashboard -->
      @if (!usage()) {
        <!-- Skeleton Loading -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          @for (i of [1,2,3,4]; track i) {
            <div class="bg-surface-2 border border-border rounded-2xl p-5 animate-pulse">
              <div class="skeleton h-3 w-20 mb-3 rounded"></div>
              <div class="skeleton h-7 w-16 mb-3 rounded"></div>
              <div class="skeleton h-2 w-full mb-2 rounded-full"></div>
              <div class="skeleton h-3 w-24 rounded"></div>
            </div>
          }
        </div>
      } @else {
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          <!-- Queries -->
          <div class="bg-surface-2 border border-border rounded-2xl p-5
                      hover:border-border-subtle transition-all duration-300 group">
            <p class="text-xs font-medium text-text-tertiary mb-2 flex items-center gap-1.5">
              <span class="w-2 h-2 rounded-full bg-accent"></span>
              Queries Today
            </p>
            <p class="text-2xl font-bold text-text-primary mb-3">{{ usage()!.queriesUsed }}</p>
            <div class="h-1.5 bg-surface-3 rounded-full overflow-hidden">
              <div class="h-full rounded-full transition-all duration-500"
                   [class]="queryPct() > 90 ? 'bg-red-500' : 'bg-gradient-to-r from-accent to-purple-500'"
                   [style.width.%]="queryPct()"></div>
            </div>
            <p class="text-xs text-text-muted mt-2">of {{ formatLimit(usage()!.queriesLimit) }}</p>
          </div>

          <!-- Documents -->
          <div class="bg-surface-2 border border-border rounded-2xl p-5
                      hover:border-border-subtle transition-all duration-300 group">
            <p class="text-xs font-medium text-text-tertiary mb-2 flex items-center gap-1.5">
              <span class="w-2 h-2 rounded-full bg-cyan-500"></span>
              Documents
            </p>
            <p class="text-2xl font-bold text-text-primary mb-3">{{ usage()!.documentsUsed }}</p>
            <div class="h-1.5 bg-surface-3 rounded-full overflow-hidden">
              <div class="h-full rounded-full transition-all duration-500"
                   [class]="docPct() > 90 ? 'bg-red-500' : 'bg-gradient-to-r from-cyan-500 to-blue-500'"
                   [style.width.%]="docPct()"></div>
            </div>
            <p class="text-xs text-text-muted mt-2">of {{ formatLimit(usage()!.documentsLimit) }}</p>
          </div>

          <!-- Projects -->
          <div class="bg-surface-2 border border-border rounded-2xl p-5
                      hover:border-border-subtle transition-all duration-300 group">
            <p class="text-xs font-medium text-text-tertiary mb-2 flex items-center gap-1.5">
              <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
              Projects
            </p>
            <p class="text-2xl font-bold text-text-primary mb-3">{{ usage()!.projectsUsed }}</p>
            <div class="h-1.5 bg-surface-3 rounded-full overflow-hidden">
              <div class="h-full rounded-full transition-all duration-500"
                   [class]="projPct() > 90 ? 'bg-red-500' : 'bg-gradient-to-r from-emerald-500 to-green-400'"
                   [style.width.%]="projPct()"></div>
            </div>
            <p class="text-xs text-text-muted mt-2">of {{ formatLimit(usage()!.projectsLimit) }}</p>
          </div>

          <!-- Storage -->
          <div class="bg-surface-2 border border-border rounded-2xl p-5
                      hover:border-border-subtle transition-all duration-300 group">
            <p class="text-xs font-medium text-text-tertiary mb-2 flex items-center gap-1.5">
              <span class="w-2 h-2 rounded-full bg-purple-500"></span>
              Storage
            </p>
            <p class="text-2xl font-bold text-text-primary mb-3">{{ formatBytes(usage()!.storageBytesUsed) }}</p>
            <div class="h-1.5 bg-surface-3 rounded-full overflow-hidden">
              <div class="h-full rounded-full transition-all duration-500"
                   [class]="storagePct() > 90 ? 'bg-red-500' : 'bg-gradient-to-r from-purple-500 to-pink-500'"
                   [style.width.%]="storagePct()"></div>
            </div>
            <p class="text-xs text-text-muted mt-2">of {{ formatBytes(usage()!.storageBytesLimit) }}</p>
          </div>
        </div>
      }

      <!-- Pricing Table -->
      <section>
        <h2 class="text-lg font-bold text-text-primary mb-5">Plans</h2>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-5">
          @for (plan of plans; track plan.tier) {
            <div class="relative flex flex-col rounded-2xl p-6 transition-all duration-300"
                 [class]="plan.popular
                   ? 'bg-gradient-to-b from-accent/10 to-surface-2 border-2 border-accent/40 shadow-lg shadow-accent/10'
                   : plan.tier === currentTier()
                     ? 'bg-surface-2 border-2 border-accent/30'
                     : 'bg-surface-2 border border-border hover:border-border-subtle'">

              @if (plan.popular) {
                <span class="absolute -top-3 left-1/2 -translate-x-1/2 px-3 py-0.5 bg-accent text-white
                             text-xs font-semibold rounded-full">
                  Popular
                </span>
              }

              <h3 class="text-lg font-bold text-text-primary">{{ plan.tier }}</h3>
              <div class="mt-3 mb-1">
                <span class="text-3xl font-bold text-text-primary">{{ plan.price }}</span>
                <span class="text-text-muted text-sm ml-1">{{ plan.period }}</span>
              </div>

              <ul class="flex-1 space-y-2.5 my-6">
                @for (f of plan.features; track f) {
                  <li class="text-sm text-text-secondary flex items-center gap-2.5">
                    <svg class="w-4 h-4 text-emerald-400 shrink-0" fill="none" viewBox="0 0 24 24"
                         stroke="currentColor" stroke-width="2.5">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                    {{ f }}
                  </li>
                }
              </ul>

              @if (plan.tier === currentTier()) {
                <button disabled
                  class="w-full py-2.5 rounded-xl bg-surface-3 text-text-muted text-sm font-medium
                         cursor-not-allowed border-none">
                  Current Plan
                </button>
              } @else if (plan.tier === 'Free') {
                <button disabled
                  class="w-full py-2.5 rounded-xl bg-surface-3 text-text-muted text-sm font-medium
                         cursor-not-allowed border-none">
                  Free
                </button>
              } @else {
                <button (click)="upgrade(plan.tier)"
                  class="w-full py-2.5 rounded-xl font-semibold text-sm cursor-pointer
                         transition-all duration-200 border-none"
                  [class]="plan.popular
                    ? 'bg-accent hover:bg-accent-hover text-white shadow-lg shadow-accent/25 hover:shadow-xl hover:shadow-accent/30 hover:-translate-y-0.5 active:translate-y-0'
                    : 'bg-surface-3 hover:bg-surface-3/80 text-text-primary'">
                  {{ currentTier() === 'Free' ? 'Upgrade' : 'Switch' }} to {{ plan.tier }}
                </button>
              }
            </div>
          }
        </div>
      </section>
    </main>
  `,
})
export class BillingComponent implements OnInit {
  private billing = inject(BillingService);
  private auth = inject(AuthService);

  subscription = this.billing.subscription;
  usage = this.billing.usage;
  currentTier = computed(() => this.auth.userTier());

  queryPct = computed(() => this.pct(this.usage()?.queriesUsed, this.usage()?.queriesLimit));
  docPct = computed(() => this.pct(this.usage()?.documentsUsed, this.usage()?.documentsLimit));
  projPct = computed(() => this.pct(this.usage()?.projectsUsed, this.usage()?.projectsLimit));
  storagePct = computed(() => this.pct(this.usage()?.storageBytesUsed, this.usage()?.storageBytesLimit));

  plans = [
    {
      tier: 'Free', price: '$0', period: 'forever', popular: false,
      features: ['100 queries/day', '10 documents', '1 project', '50 MB storage'],
    },
    {
      tier: 'Pro', price: '$29', period: '/month', popular: true,
      features: ['10,000 queries/day', '1,000 documents', '20 projects', '5 GB storage'],
    },
    {
      tier: 'Enterprise', price: '$99', period: '/month', popular: false,
      features: ['Unlimited queries', 'Unlimited documents', 'Unlimited projects', 'Unlimited storage'],
    },
  ];

  ngOnInit() {
    this.billing.loadSubscription();
    this.billing.loadUsage();
  }

  async upgrade(tier: string) {
    const url = await this.billing.createCheckoutSession(tier);
    window.location.href = url;
  }

  async manageSubscription() {
    const url = await this.billing.createPortalSession();
    window.location.href = url;
  }

  formatLimit(n: number): string {
    return n >= 2147483647 ? 'Unlimited' : n.toLocaleString();
  }

  formatBytes(bytes: number): string {
    if (bytes >= 9007199254740991) return 'Unlimited';
    if (bytes >= 1073741824) return (bytes / 1073741824).toFixed(1) + ' GB';
    if (bytes >= 1048576) return (bytes / 1048576).toFixed(1) + ' MB';
    if (bytes >= 1024) return (bytes / 1024).toFixed(0) + ' KB';
    return bytes + ' B';
  }

  private pct(used?: number, limit?: number): number {
    if (!used || !limit || limit >= 2147483647) return 0;
    return Math.min(100, (used / limit) * 100);
  }
}
