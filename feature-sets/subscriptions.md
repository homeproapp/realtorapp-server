# Subscriptions Feature Set

## Overview
This document defines the subscription management system where agents can subscribe to premium plans using Stripe for payment processing, with support for monthly/yearly billing, auto-renewal controls, and graceful cancellation handling.

## Current State
- 🔄 **Phase 1 Pending**: Stripe integration and subscription management implementation

## Requirements

### Subscription Flow
1. **Plan Selection**: Agent chooses between monthly or yearly premium plan
2. **Stripe Checkout**: Redirect to Stripe-hosted checkout for payment
3. **Webhook Processing**: Handle Stripe events for subscription lifecycle
4. **Access Control**: Enforce premium features based on active subscription
5. **Self-Service Management**: Agent can upgrade, downgrade, or cancel subscriptions

### Subscription Plans
**Free Plan (Default):**
- Limited to 1 active property
- Basic chat functionality
- Standard product search

**Premium Plan:**
- Unlimited properties
- Advanced analytics and reporting
- Priority customer support
- Enhanced product search with bulk export

### Billing Options
- **Monthly**: TBD CAD/month
- **Yearly**: TBD CAD/year (discount TBD)
- **Auto-renewal**: Configurable by agent (enabled by default)
- **Proration**: Handle mid-cycle plan changes with Stripe proration

### Cancellation Policy
- **Immediate Cancellation**: Auto-renewal disabled, service continues until period end
- **Grace Period**: 7-day grace period after failed payment before downgrade
- **Reactivation**: Allow reactivation within grace period without losing data

## API Endpoints

### Subscription Management
- GET `/api/subscriptions/v1/plans` - List available plans and pricing
- GET `/api/subscriptions/v1/current` - Get agent's current subscription status
- POST `/api/subscriptions/v1/checkout` - Create Stripe checkout session
- POST `/api/subscriptions/v1/portal` - Create Stripe customer portal session
- PUT `/api/subscriptions/v1/auto-renewal` - Toggle auto-renewal setting
- POST `/api/subscriptions/v1/cancel` - Cancel subscription (disable auto-renewal)

### Webhook Endpoint
- POST `/api/webhooks/stripe` - Handle Stripe webhook events

### GET /api/subscriptions/v1/current Response
```json
{
  "subscriptionId": "sub_1234567890",
  "planType": "premium",
  "billingCycle": "yearly",
  "status": "active",
  "currentPeriodStart": "2024-01-15T00:00:00Z",
  "currentPeriodEnd": "2025-01-15T00:00:00Z",
  "autoRenewal": true,
  "cancelAtPeriodEnd": false,
  "trialEnd": null,
  "nextBillingDate": "2025-01-15T00:00:00Z",
  "amount": 0, // TBD - amount in cents
  "currency": "cad",
  "paymentMethod": {
    "last4": "4242",
    "brand": "visa",
    "expiryMonth": 12,
    "expiryYear": 2025
  }
}
```

### POST /api/subscriptions/v1/checkout Request
```json
{
  "planType": "premium",
  "billingCycle": "monthly",
  "successUrl": "https://app.realtor.com/subscription/success",
  "cancelUrl": "https://app.realtor.com/subscription/cancel"
}
```

### POST /api/subscriptions/v1/checkout Response
```json
{
  "checkoutSessionId": "cs_test_1234567890",
  "checkoutUrl": "https://checkout.stripe.com/pay/cs_test_1234567890"
}
```

## Database Schema

### Subscriptions Table
```sql
CREATE TABLE subscriptions (
    subscription_id SERIAL PRIMARY KEY,
    agent_id INTEGER NOT NULL REFERENCES agents(agent_id),
    stripe_subscription_id VARCHAR(255) UNIQUE NOT NULL,
    stripe_customer_id VARCHAR(255) NOT NULL,
    plan_type VARCHAR(50) NOT NULL CHECK (plan_type IN ('free', 'premium')),
    billing_cycle VARCHAR(20) NOT NULL CHECK (billing_cycle IN ('monthly', 'yearly')),
    status VARCHAR(50) NOT NULL CHECK (status IN ('active', 'canceled', 'past_due', 'unpaid', 'incomplete')),
    current_period_start TIMESTAMPTZ NOT NULL,
    current_period_end TIMESTAMPTZ NOT NULL,
    cancel_at_period_end BOOLEAN NOT NULL DEFAULT FALSE,
    auto_renewal BOOLEAN NOT NULL DEFAULT TRUE,
    trial_end TIMESTAMPTZ,
    amount INTEGER NOT NULL, -- Amount in cents
    currency VARCHAR(3) NOT NULL DEFAULT 'cad',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_subscriptions_agent_id ON subscriptions(agent_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_subscriptions_stripe_subscription_id ON subscriptions(stripe_subscription_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_subscriptions_status ON subscriptions(status) WHERE deleted_at IS NULL;
CREATE INDEX idx_subscriptions_current_period_end ON subscriptions(current_period_end) WHERE deleted_at IS NULL;
```

### Subscription Events Table (Audit Log)
```sql
CREATE TABLE subscription_events (
    event_id SERIAL PRIMARY KEY,
    subscription_id INTEGER NOT NULL REFERENCES subscriptions(subscription_id),
    stripe_event_id VARCHAR(255) UNIQUE NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_data JSONB NOT NULL,
    processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_subscription_events_subscription_id ON subscription_events(subscription_id);
CREATE INDEX idx_subscription_events_stripe_event_id ON subscription_events(stripe_event_id);
CREATE INDEX idx_subscription_events_event_type ON subscription_events(event_type);
```

### Payment Methods Table
```sql
CREATE TABLE payment_methods (
    payment_method_id SERIAL PRIMARY KEY,
    agent_id INTEGER NOT NULL REFERENCES agents(agent_id),
    stripe_payment_method_id VARCHAR(255) UNIQUE NOT NULL,
    stripe_customer_id VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL CHECK (type IN ('card', 'bank_account')),
    last4 VARCHAR(4),
    brand VARCHAR(50),
    expiry_month INTEGER,
    expiry_year INTEGER,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_payment_methods_agent_id ON payment_methods(agent_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_payment_methods_stripe_payment_method_id ON payment_methods(stripe_payment_method_id) WHERE deleted_at IS NULL;
```

## Service Architecture

### ISubscriptionService
**Responsibilities:**
- Manage subscription lifecycle and status
- Coordinate with Stripe for billing operations
- Handle plan upgrades, downgrades, and cancellations
- Enforce subscription-based access controls
- Generate subscription analytics and reports

**Key Methods:**
- `GetCurrentSubscriptionAsync(agentId)` - Get agent's active subscription
- `CreateCheckoutSessionAsync(agentId, planType, billingCycle)` - Initialize Stripe checkout
- `CreatePortalSessionAsync(agentId)` - Generate customer portal access
- `UpdateAutoRenewalAsync(agentId, autoRenewal)` - Toggle auto-renewal setting
- `CancelSubscriptionAsync(agentId)` - Cancel subscription (disable auto-renewal)
- `IsFeatureAvailableAsync(agentId, feature)` - Check premium feature access
- `GetSubscriptionMetricsAsync(agentId)` - Usage analytics for agent

### IStripeWebhookService
**Responsibilities:**
- Process Stripe webhook events securely
- Update local subscription data based on Stripe events
- Handle payment failures and retry logic
- Manage subscription state transitions
- Log all webhook events for audit trail

**Key Methods:**
- `ProcessWebhookAsync(payload, signature)` - Main webhook processor
- `HandleSubscriptionCreatedAsync(subscription)` - New subscription setup
- `HandleSubscriptionUpdatedAsync(subscription)` - Subscription changes
- `HandleSubscriptionDeletedAsync(subscription)` - Subscription cancellation
- `HandleInvoicePaymentSucceededAsync(invoice)` - Successful payment
- `HandleInvoicePaymentFailedAsync(invoice)` - Failed payment handling

### IPaymentMethodService
**Responsibilities:**
- Manage customer payment methods
- Handle payment method updates and deletions
- Coordinate with Stripe for payment method operations
- Maintain payment method metadata and preferences

**Key Methods:**
- `GetPaymentMethodsAsync(agentId)` - List agent's payment methods
- `SetDefaultPaymentMethodAsync(agentId, paymentMethodId)` - Update default method
- `DeletePaymentMethodAsync(agentId, paymentMethodId)` - Remove payment method
- `SyncPaymentMethodsAsync(agentId)` - Sync with Stripe data

## Stripe Integration

### Webhook Events to Handle
- `customer.subscription.created` - New subscription
- `customer.subscription.updated` - Subscription changes
- `customer.subscription.deleted` - Subscription cancelled
- `invoice.payment_succeeded` - Successful payment
- `invoice.payment_failed` - Failed payment
- `customer.subscription.trial_will_end` - Trial ending notification
- `payment_method.attached` - New payment method added

### Stripe Configuration
```json
{
  "Stripe": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "PriceIds": {
      "PremiumMonthly": "price_premium_monthly_cad",
      "PremiumYearly": "price_premium_yearly_cad"
    },
    "SuccessUrl": "https://app.realtor.com/subscription/success",
    "CancelUrl": "https://app.realtor.com/subscription/cancel"
  }
}
```

## Access Control Implementation

### Subscription-Based Feature Gates
```csharp
public enum PremiumFeature
{
    UnlimitedProperties,
    AdvancedAnalytics,
    PrioritySupport,
    BulkProductExport,
    CustomReports
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequiresPremiumAttribute : Attribute
{
    public PremiumFeature Feature { get; }

    public RequiresPremiumAttribute(PremiumFeature feature)
    {
        Feature = feature;
    }
}
```

### Usage Examples
```csharp
[RequiresPremium(PremiumFeature.UnlimitedProperties)]
public async Task<IActionResult> CreateProperty(CreatePropertyCommand command)
{
    // Only premium subscribers can create unlimited properties
}

[RequiresPremium(PremiumFeature.AdvancedAnalytics)]
public async Task<IActionResult> GetAdvancedReports(GetReportsQuery query)
{
    // Premium feature - advanced reporting
}
```

## Contract Structure (RealtorApp.Contracts)

### Commands
**Commands/Subscriptions/Requests/**
- `CreateCheckoutSessionCommand`
- `CreatePortalSessionCommand`
- `UpdateAutoRenewalCommand`
- `CancelSubscriptionCommand`

**Commands/Subscriptions/Responses/**
- `CreateCheckoutSessionCommandResponse`
- `CreatePortalSessionCommandResponse`
- `UpdateAutoRenewalCommandResponse`
- `CancelSubscriptionCommandResponse`

### Queries
**Queries/Subscriptions/Requests/**
- `GetSubscriptionPlansQuery`
- `GetCurrentSubscriptionQuery`
- `GetSubscriptionMetricsQuery`

**Queries/Subscriptions/Responses/**
- `GetSubscriptionPlansQueryResponse`
- `GetCurrentSubscriptionQueryResponse`
- `GetSubscriptionMetricsQueryResponse`

### Supporting Models
**Subscriptions/**
- `SubscriptionResponse`
- `PlanResponse`
- `PaymentMethodResponse`
- `SubscriptionMetricsResponse`

## Security Considerations

### Webhook Security
- **Signature Verification**: Validate Stripe webhook signatures
- **Idempotency**: Handle duplicate webhook events gracefully
- **Event Ordering**: Process events in correct chronological order
- **Timeout Protection**: Implement webhook processing timeouts

### Payment Data Security
- **PCI Compliance**: Never store sensitive payment data locally
- **Stripe Customer IDs**: Use Stripe customer IDs for payment operations
- **Token-based Access**: Use Stripe payment method tokens only
- **Audit Logging**: Log all subscription and payment events

### Access Control
- **Feature Gating**: Enforce premium features at API level
- **Subscription Validation**: Verify active subscription for premium operations
- **Grace Period**: Allow feature access during payment failure grace period
- **Rate Limiting**: Apply different rate limits based on subscription tier

## Implementation Phases

### Phase 1: Core Subscription Management
- Database schema creation and migrations
- Stripe integration services
- Basic subscription CRUD operations
- Webhook event processing
- Simple feature gating

### Phase 2: Advanced Features
- Customer portal integration
- Proration handling for plan changes
- Advanced analytics and reporting
- Failed payment retry logic
- Dunning management

### Phase 3: Optimization & Analytics
- Subscription metrics dashboard
- Churn analysis and prevention
- A/B testing for pricing
- Revenue optimization
- Customer lifecycle management

## Success Criteria
- **Payment Processing**: 99.9% successful payment processing rate
- **Feature Access**: Immediate premium feature access after payment
- **Cancellation Handling**: Graceful service continuation until period end
- **Webhook Reliability**: 100% webhook event processing with retry logic
- **Security Compliance**: Full PCI DSS compliance for payment handling
- **User Experience**: Seamless subscription management with minimal friction

## Edge Cases
- **Failed Payments**: Handle declined cards with retry logic and grace period
- **Plan Changes**: Mid-cycle upgrades/downgrades with proper proration
- **Refunds**: Process refunds and adjust subscription accordingly
- **Account Suspension**: Handle suspended Stripe accounts gracefully
- **Webhook Failures**: Retry webhook processing with exponential backoff
- **Duplicate Events**: Idempotent webhook processing to prevent double processing
- **Currency Changes**: Handle currency conversion for international customers
- **Tax Compliance**: Automatic tax calculation and collection via Stripe Tax