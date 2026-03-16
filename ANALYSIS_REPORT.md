═══════════════════════════════════════════════════════════════════════════════
  MARKETPLACE CODEBASE EXPLORATION - SUBSCRIPTION & ANALYTICS SYSTEM ANALYSIS
═══════════════════════════════════════════════════════════════════════════════

## 1. SELLER SUBSCRIPTION SYSTEM - OVERVIEW

### 1.1 Subscription Types
Three tiers exist, defined in: MarketplaceArtesanato.Core/Entities/Enums/SellerPlan.cs
  • Basic (FREE)
  • Pro (.99/month)
  • Premium (.90/month)

### 1.2 SellerSubscription Entity Location
Entity: MarketplaceArtesanato.Core/Entities/SellerSubscription.cs
Table: SellerSubscriptions (SQL Server)
Relationship: One-to-one with Sellers table (one subscription per seller)

### 1.3 Subscription Fields & Capabilities
+─────────────────────────┬────────┬──────┬──────────┐
│ Feature                 │ Basic  │ Pro  │ Premium  │
├─────────────────────────┼────────┼──────┼──────────┤
│ Price/Month             │ FREE   │.99│ .90   │
│ Commission Rate         │ 12%    │ 9%   │   5%     │
│ Can Highlight Products  │   ❌   │  ✅  │   ✅     │
│ Highlight Limit         │   0    │  8   │   15     │
│ Verified Badge          │   ❌   │  ✅  │   ✅     │
│ Advanced Analytics      │   ❌   │  ✅  │   ✅     │
│ Priority Support        │   ❌   │  ❌  │   ✅     │
│ Can Create Coupons      │   ❌   │ ✅(3)│   ✅(∞)  │
└─────────────────────────┴────────┴──────┴──────────┘

Entity Properties:
  • Plan: SellerPlan enum (Basic/Pro/Premium)
  • StartedAt: DateTime - when subscription began
  • ExpiresAt: DateTime? - optional expiration
  • IsActive: bool - current status
  • CommissionRate: decimal(5,2)
  • CanHighlightProducts: bool
  • HasVerifiedBadge: bool
  • HasAdvancedAnalytics: bool ← KEY FLAG (exists but NOT enforced)
  • HasPrioritySupport: bool
  • MonthlyPrice: decimal
  • HighlightLimit: int

### 1.4 Subscription Tracking & Validation

Service: MarketplaceArtesanato.Services/Services/SellerSubscribeService.cs
Interface: MarketplaceArtesanato.Core/Interfaces/ISellerSubscriptionService.cs

Methods:
  ✓ GetActiveSubscriptionAsync(Guid sellerId)
  ✓ SubscribeAsync(Guid sellerId, SellerPlan plan)
  ✓ ChangePlanAsync(Guid sellerId, SellerPlan newPlan)
  ✓ CreateCheckoutSessionAsync(Guid sellerId, SellerPlan plan)
  ✓ CancelAsync(Guid sellerId)

Validation Rules:
  ✓ Only 1 active subscription per seller (unique constraint)
  ✓ Basic plan activates immediately (free)
  ✓ Pro/Premium require Stripe checkout
  ✓ Cancellation defaults seller to Basic plan
  ✓ Plan changes update all subscription properties atomically

API Endpoints:
  POST   /api/sellers/subscription/checkout       Create paid plan checkout
  GET    /api/sellers/subscription                Get current subscription
  POST   /api/sellers/subscription                Subscribe to plan
  PUT    /api/sellers/subscription                Change plan
  DELETE /api/sellers/subscription                Cancel subscription

───────────────────────────────────────────────────────────────────────────────
## 2. CURRENT ANALYTICS IMPLEMENTATION

### 2.1 Seller Dashboard Analytics
Location: FrontEnd/frontend-marketplace/src/components/seller-dashboard/

Current Metrics Displayed:
  • Total Revenue (current month)
  • Previous Revenue (for comparison)
  • Total Sales Count
  • Active Products Count
  • Daily Revenue Chart (7 days)

Backend Service: MarketplaceArtesanato.Services/Services/SellerService.cs
  Method: GetDashboardAsync(Guid userId)

DTO: SellerDashboardDto
  - SellerId: Guid
  - TotalRevenue: decimal
  - PreviousRevenue: decimal
  - TotalSales: int
  - PreviousSales: int
  - ActiveProducts: int
  - DailyRevenue: List<DailyRevenueDto>

ACCESS: [Authorize(Roles = "Seller")] - All sellers can access own dashboard
RESTRICTION: ❌ NO subscription-tier enforcement (all sellers see same data)

### 2.2 Coupon Analytics
Location: FrontEnd/frontend-marketplace/src/components/seller-coupon-analytics/

Metrics Available:
  • Total Customer Savings (R$)
  • Active Coupons Count
  • Average ROI (%)
  • Conversion Rate (%)
  • Monthly Trend (%)
  • Top 5 Performing Coupons (ranked by ROI)
  • Bottom 5 Underperforming Coupons
  • Individual coupon metrics: usages, conversion, avg order value

Backend Service: MarketplaceArtesanato.Services/Services/CouponAnalyticsService.cs

Key Methods:
  ✓ GetCouponAnalyticsDashboardAsync(Guid sellerId)
  ✓ GetSellerCouponStatsAsync(Guid sellerId)
  ✓ GetCouponPerformanceAsync(Guid couponId, startDate, endDate)
  ✓ GetSellerCouponsComparisonAsync(Guid sellerId, topN)
  ✓ CalculateROIAsync(Guid couponId)

ACCESS: [Authorize(Roles = "Seller")] - Filtered by CreatorSellerId
RESTRICTION: ❌ NO subscription-tier enforcement

### 2.3 Admin Platform Analytics
Location: MarketplaceArtesanato.API/Controller/AnalyticsController.cs

Endpoints (all require [Authorize(Roles = "Admin")]):
  GET /api/analytics/platform           → Platform-wide metrics (GMV, revenue)
  GET /api/analytics/top-products       → Top 10 products by sales
  GET /api/analytics/users              → User breakdown by role
  GET /api/analytics/sales-period       → 12-month sales trend
  GET /api/analytics/category-distribution → Products by category
  GET /api/analytics/health             → Platform health score (0-100)
  GET /api/analytics/sellers            → Seller performance & commissions
  GET /api/analytics/conversion-funnel  → Visitors → Cart → Checkout → Completed

Metrics Provided:
  • Total GMV (Gross Merchandise Value)
  • Total Orders & Revenue
  • User Counts (total, buyers, sellers, admins)
  • Commission calculations by seller
  • Platform health indicators
  • Conversion funnel stages

───────────────────────────────────────────────────────────────────────────────
## 3. EXISTING TIER-BASED RESTRICTIONS

### Currently Implemented:
1. PRODUCT HIGHLIGHTING
   • Basic: Cannot highlight ANY products (CanHighlightProducts = false)
   • Pro: Up to 8 highlighted products
   • Premium: Up to 15 highlighted products
   Code: ProductService.cs checks subscription before allowing highlights

2. COUPON CREATION
   • Basic: Forbid - "Plano Basic não permite criar cupons"
   • Pro: Allow but max 3 ACTIVE coupons
   • Premium: Allow unlimited coupons
   Code: CouponsController.cs validates plan at POST /api/coupons/seller

3. ANALYTICS ACCESS
   • Exists but NOT ENFORCED:
   Flag HasAdvancedAnalytics is set correctly in database but:
   ❌ No API-level checks
   ❌ No frontend conditional rendering
   ❌ Basic sellers get same data as Pro/Premium

───────────────────────────────────────────────────────────────────────────────
## 4. DATABASE SCHEMA

### SellerSubscriptions Table Structure

Table Name: SellerSubscriptions

Columns:
  Id                    UNIQUEIDENTIFIER PRIMARY KEY
  SellerId              UNIQUEIDENTIFIER NOT NULL (UNIQUE INDEX)
  Plan                  INT NOT NULL (default: 0)
  StartedAt             DATETIME2 NOT NULL
  ExpiresAt             DATETIME2 NULL
  IsActive              BIT NOT NULL (default: 1)
  CommissionRate        DECIMAL(5,2) NOT NULL
  CanHighlightProducts  BIT NOT NULL
  MonthlyPrice          DECIMAL NOT NULL
  HighlightLimit        INT NOT NULL
  HasVerifiedBadge      BIT NOT NULL
  HasAdvancedAnalytics  BIT NOT NULL ← KEY FLAG
  HasPrioritySupport    BIT NOT NULL

Foreign Key:
  SellerId → Sellers.Id (CASCADE DELETE)

Indexes:
  • SellerId (UNIQUE)
  • Plan (regular index for filtering)

Relationship:
  Sellers.SellerSubscription (one-to-one, optional navigation property)

### Related Tables for Analytics:
  • Orders: Core transaction data
  • OrderItems: Product sales details (used for revenue calc)
  • Coupons: Coupon definitions (CreatorSellerId = seller)
  • CouponUsages: Coupon usage tracking (for ROI/conversion calc)
  • Products: Product inventory (SellerId = seller)

───────────────────────────────────────────────────────────────────────────────
## 5. RECOMMENDATION: PRO/PREMIUM ANALYTICS IMPLEMENTATION

### Current Gaps:
❌ HasAdvancedAnalytics flag exists but is NOT enforced
❌ No API-level validation of subscription tier for analytics
❌ No frontend guards preventing Basic plan access to Pro features
❌ All analytics endpoints return same data regardless of plan

### Proposed Pro Tier Analytics (New Features):
  ✓ Previous month comparison (% change)
  ✓ Customer analysis (repeat rate, lifetime value)
  ✓ Product performance ranking (top/bottom products)
  ✓ 30/60/90 day revenue trends
  ✓ Hourly revenue distribution
  ✓ Average order value trends
  ✓ Coupon effectiveness metrics
  ✓ Low stock alerts
  ✓ Customer geographic data (if available)

### Proposed Premium Tier Analytics (Advanced Features):
  ✓ All Pro features PLUS:
  ✓ AI-powered insights & recommendations
  ✓ 30/90-day revenue forecasting
  ✓ Customer segmentation analysis
  ✓ Seasonal trend analysis
  ✓ Custom date range reports
  ✓ Scheduled email reports
  ✓ Advanced filtering & custom dashboards
  ✓ Export to CSV/PDF
  ✓ Real-time alerts on key metrics

### Implementation Steps:

PHASE 1: Enforce Tier-Based Access (1-2 days)
  1. Add method to check subscription in SellerService:
     bool CanAccessAdvancedAnalytics(Guid sellerId)
  
  2. Create separate DTOs:
     - BasicSellerDashboardDto (limited fields)
     - AdvancedSellerDashboardDto (all fields)
  
  3. Modify SellersController.GetDashboard():
     if (seller.Subscription?.HasAdvancedAnalytics)
         return advancedDTO;
     return basicDTO;

PHASE 2: Add Pro Analytics Service (2-3 days)
  1. Create: AdvancedAnalyticsService.cs
     Methods:
     - GetCustomerAnalysis(Guid sellerId)
     - GetProductPerformance(Guid sellerId)
     - GetRevenueComparison(Guid sellerId, period)
     - GetTrendAnalysis(Guid sellerId, days)
  
  2. Create DTOs:
     - ProAnalyticsDashboardDto (includes all Pro metrics)
  
  3. Add endpoint: GET /api/sellers/analytics/advanced

PHASE 3: Premium Features (3-4 days)
  1. Implement forecasting (simple polynomial regression)
  2. Create ReportingService for scheduled reports
  3. Add PDF export using library (e.g., iText, QuestPDF)
  4. Create AlertService for real-time notifications
  5. Create: PremiumAnalyticsDashboardDto

PHASE 4: Frontend Guards (1 day)
  1. Add conditional components:
     <div *ngIf="subscription.hasAdvancedAnalytics">
       <app-advanced-analytics />
     </div>
  
  2. Display upgrade CTA for Basic sellers
  3. Implement feature-locked UI states

### Database Changes (Optional):
  CREATE TABLE AdvancedAnalyticsSnapshots (
      Id UNIQUEIDENTIFIER PRIMARY KEY,
      SellerId UNIQUEIDENTIFIER,
      SnapshotDate DATETIME2,
      RevenueTrend DECIMAL,
      ConversionRate DECIMAL,
      AvgOrderValue DECIMAL,
      CustomerRetentionRate DECIMAL,
      CreatedAt DATETIME2,
      FOREIGN KEY(SellerId) REFERENCES Sellers(Id)
  )

  CREATE TABLE AnalyticsAlerts (
      Id UNIQUEIDENTIFIER PRIMARY KEY,
      SellerId UNIQUEIDENTIFIER,
      AlertType NVARCHAR(100), -- 'LowStock', 'PriceSuggestion'
      Message NVARCHAR(MAX),
      IsRead BIT,
      CreatedAt DATETIME2,
      FOREIGN KEY(SellerId) REFERENCES Sellers(Id)
  )

───────────────────────────────────────────────────────────────────────────────
## SUMMARY TABLE: What Exists vs What Needs Implementation

Feature                          Status      Location
────────────────────────────────────────────────────────────────
Subscription Tiers (3 types)     ✅ Done      SellerPlan.cs
Tier Capabilities Defined        ✅ Done      SellerSubscribeService.cs
Has Advanced Analytics Flag      ✅ Done      SellerSubscription.cs (DB)
Basic Seller Dashboard           ✅ Done      SellerDashboardComponent
Coupon Analytics                 ✅ Done      CouponAnalyticsService
Tier-Based Coupon Limits         ✅ Done      CouponsController.cs
Tier-Based Product Highlights    ✅ Done      ProductService.cs
────────────────────────────────────────────────────────────────
Enforce Advanced Analytics Flag  ❌ Missing   Needs API/Frontend checks
Pro-Tier Analytics Service       ❌ Missing   Needs new AdvancedAnalyticsService
Premium-Tier Analytics Service   ❌ Missing   Needs new PremiumAnalyticsService
Custom Reporting Service         ❌ Missing   Needs ReportingService
Export/PDF Functionality         ❌ Missing   Needs export service
Predictive Analytics             ❌ Missing   Needs forecasting library

═══════════════════════════════════════════════════════════════════════════════
KEY FILES REFERENCE
═══════════════════════════════════════════════════════════════════════════════

BACKEND - Subscription:
  • MarketplaceArtesanato.Core/Entities/Enums/SellerPlan.cs
  • MarketplaceArtesanato.Core/Entities/SellerSubscription.cs
  • MarketplaceArtesanato.Core/Interfaces/ISellerSubscriptionService.cs
  • MarketplaceArtesanato.Services/Services/SellerSubscribeService.cs
  • MarketplaceArtesanato.API/Controller/SellersController.cs (subscription endpoints)

BACKEND - Analytics:
  • MarketplaceArtesanato.Services/Services/AnalyticsService.cs (Admin)
  • MarketplaceArtesanato.Services/Services/CouponAnalyticsService.cs (Seller)
  • MarketplaceArtesanato.Services/Services/SellerService.cs (GetDashboardAsync)
  • MarketplaceArtesanato.API/Controller/AnalyticsController.cs
  • MarketplaceArtesanato.API/Controller/CouponsController.cs (coupon analytics)

BACKEND - Database:
  • MarketplaceArtesanato.Data/Data/ArtesianDbContext.cs
  • MarketplaceArtesanato.Data/Migrations/20260111203628_AddPricingSystem.Designer.cs

FRONTEND - Seller:
  • FrontEnd/frontend-marketplace/src/services/seller/seller.service.ts
  • FrontEnd/frontend-marketplace/src/components/seller-dashboard/
  • FrontEnd/frontend-marketplace/src/components/seller-coupon-analytics/

DTOs:
  • MarketplaceArtesanato.Core/Entities/DTO/SellerDashboardDto.cs
  • MarketplaceArtesanato.Core/Entities/DTO/AnalyticsDto.cs

═══════════════════════════════════════════════════════════════════════════════
