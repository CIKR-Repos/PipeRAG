import { Component, inject, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../shared/components/navbar/navbar';
import { BillingService, UsageInfo } from '../../core/services/billing.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  template: `
    <app-navbar />
    <main class="max-w-5xl mx-auto px-6 py-8">
      <h1 class="text-3xl font-bold text-white mb-8">Billing & Usage</h1>

      <!-- Current Plan -->
      <section class="bg-dark-card border border-white/10 rounded-xl p-6 mb-8">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-white/60 mb-1">Current Plan</p>
            <h2 class="text-2xl font-bold text-white">{{ currentTier() }}</h2>
            @if (subscription()?.currentPeriodEnd) {
              <p class="text-sm text-white/50 mt-1">
                Renews {{ subscription()!.currentPeriodEnd | date:'mediumDate' }}
              </p>
            }
          </div>
          @if (currentTier() !== 'Free') {
            <button (click)="manageSubscription()"
                    class="px-4 py-2 bg-white/10 text-white border border-white/20 rounded-lg hover:bg-white/20 transition">
              Manage Subscription
            </button>
          }
        </div>
      </section>

      <!-- Usage Dashboard -->
      @if (usage()) {
        <section class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          <div class="bg-dark-card border border-white/10 rounded-xl p-5">
            <p class="text-sm text-white/60 mb-2">Queries Today</p>
            <p class="text-2xl font-bold text-white">{{ usage()!.queriesUsed }}</p>
            <div class="mt-2 h-2 bg-white/10 rounded-full overflow-hidden">
              <div class="h-full bg-accent rounded-full transition-all"
                   [style.width.%]="queryPct()"></div>
            </div>
            <p class="text-xs text-white/40 mt-1">of {{ formatLimit(usage()!.queriesLimit) }}</p>
          </div>
          <div class="bg-dark-card border border-white/10 rounded-xl p-5">
            <p class="text-sm text-white/60 mb-2">Documents</p>
            <p class="text-2xl font-bold text-white">{{ usage()!.documentsUsed }}</p>
            <div class="mt-2 h-2 bg-white/10 rounded-full overflow-hidden">
              <div class="h-full bg-cyan rounded-full transition-all"
                   [style.width.%]="docPct()"></div>
            </div>
            <p class="text-xs text-white/40 mt-1">of {{ formatLimit(usage()!.documentsLimit) }}</p>
          </div>
          <div class="bg-dark-card border border-white/10 rounded-xl p-5">
            <p class="text-sm text-white/60 mb-2">Projects</p>
            <p class="text-2xl font-bold text-white">{{ usage()!.projectsUsed }}</p>
            <div class="mt-2 h-2 bg-white/10 rounded-full overflow-hidden">
              <div class="h-full bg-green-400 rounded-full transition-all"
                   [style.width.%]="projPct()"></div>
            </div>
            <p class="text-xs text-white/40 mt-1">of {{ formatLimit(usage()!.projectsLimit) }}</p>
          </div>
          <div class="bg-dark-card border border-white/10 rounded-xl p-5">
            <p class="text-sm text-white/60 mb-2">Storage</p>
            <p class="text-2xl font-bold text-white">{{ formatBytes(usage()!.storageBytesUsed) }}</p>
            <div class="mt-2 h-2 bg-white/10 rounded-full overflow-hidden">
              <div class="h-full bg-purple-400 rounded-full transition-all"
                   [style.width.%]="storagePct()"></div>
            </div>
            <p class="text-xs text-white/40 mt-1">of {{ formatBytes(usage()!.storageBytesLimit) }}</p>
          </div>
        </section>
      }

      <!-- Pricing Table -->
      <section>
        <h2 class="text-xl font-bold text-white mb-4">Plans</h2>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          @for (plan of plans; track plan.tier) {
            <div class="bg-dark-card border rounded-xl p-6 flex flex-col"
                 [class]="plan.tier === currentTier() ? 'border-accent' : 'border-white/10'">
              <h3 class="text-lg font-bold text-white">{{ plan.tier }}</h3>
              <p class="text-3xl font-bold text-white mt-2">{{ plan.price }}</p>
              <p class="text-sm text-white/50 mb-4">{{ plan.period }}</p>
              <ul class="flex-1 space-y-2 mb-6">
                @for (f of plan.features; track f) {
                  <li class="text-sm text-white/70 flex items-center gap-2">
                    <span class="text-green-400">✓</span> {{ f }}
                  </li>
                }
              </ul>
              @if (plan.tier === currentTier()) {
                <button disabled class="w-full py-2 rounded-lg bg-white/10 text-white/50 cursor-not-allowed">
                  Current Plan
                </button>
              } @else if (plan.tier === 'Free') {
                <button disabled class="w-full py-2 rounded-lg bg-white/10 text-white/50 cursor-not-allowed">
                  Free
                </button>
              } @else {
                <button (click)="upgrade(plan.tier)"
                        class="w-full py-2 rounded-lg bg-accent text-white font-semibold hover:bg-accent/80 transition">
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
      tier: 'Free', price: '$0', period: 'forever',
      features: ['100 queries/day', '10 documents', '1 project', '50 MB storage'],
    },
    {
      tier: 'Pro', price: '$29', period: '/month',
      features: ['10,000 queries/day', '1,000 documents', '20 projects', '5 GB storage'],
    },
    {
      tier: 'Enterprise', price: '$99', period: '/month',
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
