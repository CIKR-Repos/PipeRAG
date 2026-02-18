# PR #9: Billing + Tier Enforcement (Stripe Integration)

## Overview
Stripe-powered billing with tiered usage enforcement for PipeRAG.

## Tier Limits

| Feature | Free | Pro ($29/mo) | Enterprise ($99/mo) |
|---------|------|-------------|-------------------|
| Queries/day | 100 | 10,000 | Unlimited |
| Documents | 10 | 1,000 | Unlimited |
| Projects | 1 | 20 | Unlimited |
| Storage | 50 MB | 5 GB | Unlimited |

## API Endpoints

- `POST /api/billing/create-checkout-session` — Start Stripe Checkout for upgrade
- `POST /api/billing/create-portal-session` — Open Stripe Customer Portal
- `POST /api/billing/webhook` — Stripe webhook handler
- `GET /api/billing/subscription` — Current subscription status
- `GET /api/billing/usage` — Current usage stats

## Architecture

### Backend
- **StripeService** — Handles Stripe customer creation, checkout sessions, portal sessions, webhook processing
- **UsageTrackingService** — Tracks queries/day, documents, projects, storage per user
- **TierEnforcementMiddleware** — Intercepts POST requests and checks tier limits before allowing operations
- **Entities**: `Subscription`, `UsageRecord` tables

### Frontend
- **Billing page** (`/billing`) — Shows current plan, usage dashboard with progress bars, pricing comparison table
- **BillingService** — Angular service for billing API calls

## Webhook Events Handled
- `checkout.session.completed` — Activate subscription after payment
- `customer.subscription.updated` — Sync subscription status changes
- `customer.subscription.deleted` — Downgrade to Free tier on cancellation

## Configuration
Required in `appsettings.json`:
```json
{
  "Stripe": {
    "SecretKey": "sk_...",
    "WebhookSecret": "whsec_...",
    "ProPriceId": "price_...",
    "EnterprisePriceId": "price_..."
  }
}
```

> ⚠️ **Security:** Never commit real Stripe keys to source control. Use environment variables, user secrets (`dotnet user-secrets`), or a key vault for production deployments.
