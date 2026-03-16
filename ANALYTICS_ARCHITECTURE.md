# ANALYTICS ARCHITECTURE & CODE ORGANIZATION

## Project Structure

\\\
MarketplaceArtesanato/
├── BackEnd/
│   ├── MarketplaceArtesanato.API/
│   │   └── Controller/
│   │       ├── AnalyticsController.cs (Public + Admin endpoints)
│   │       └── SellerAnalyticsAdvancedController.cs (Seller Pro/Premium endpoints)
│   │
│   ├── MarketplaceArtesanato.Services/
│   │   └── Services/
│   │       ├── AnalyticsService.cs (Platform analytics logic)
│   │       ├── SellerAnalyticsService.cs (Basic seller analytics)
│   │       ├── SellerAnalyticsAdvancedService.cs (Advanced seller analytics)
│   │       └── CouponAnalyticsService.cs (Coupon effectiveness metrics)
│   │
│   └── MarketplaceArtesanato.Core/
│       ├── Interfaces/
│       │   ├── IAnalyticsService.cs
│       │   ├── ISellerAnalyticsService.cs
│       │   ├── ISellerAnalyticsAdvancedService.cs
│       │   └── ICouponAnalyticsService.cs
│       │
│       └── Entities/
│           ├── DTO/
│           │   ├── AnalyticsDto.cs (Platform DTOs)
│           │   └── AnalyticsAdvancedDtos.cs (Advanced seller DTOs)
│           │
│           └── Models/Requests/
│               └── AnalyticsEventDto.cs (Frontend event DTOs)
\\\

---

## Class Hierarchy & Dependencies

### AnalyticsController
**File:** MarketplaceArtesanato.API/Controller/AnalyticsController.cs
**Lines:** 234
**Dependencies:**
- IAnalyticsService
- HttpContext (for User-Agent, IP, timestamps)

**Methods:**
1. LogAnalyticsEvents(AnalyticsEventBatchDto) - POST /api/analytics/events [Public]
2. GetPlatformAnalytics() - GET /api/analytics/platform [Admin]
3. GetTopProducts(limit=10) - GET /api/analytics/top-products [Admin]
4. GetUserAnalytics() - GET /api/analytics/users [Admin]
5. GetSalesByPeriod() - GET /api/analytics/sales-period [Admin]
6. GetCategoryDistribution() - GET /api/analytics/category-distribution [Admin]
7. GetPlatformHealth() - GET /api/analytics/health [Admin]
8. GetSellerPerformance() - GET /api/analytics/sellers [Admin]
9. GetConversionFunnel() - GET /api/analytics/conversion-funnel [Admin]

**Key Features:**
- No persistence layer (events logged to Debug.WriteLine only)
- Basic validation (max 100 events per batch)
- Security: Client IP, User-Agent captured
- AllowAnonymous for event posting

---

### SellerAnalyticsAdvancedController
**File:** MarketplaceArtesanato.API/Controller/SellerAnalyticsAdvancedController.cs
**Lines:** 352
**Dependencies:**
- ISellerAnalyticsAdvancedService
- IAuthorizationService
- ILogger<SellerAnalyticsAdvancedController>

**Methods:**
1. CheckAccess() - GET [Seller role, no plan restriction]
2. GetAdvancedDashboard(days=30) - GET /dashboard [Pro/Premium policy]
3. GetConversionMetrics(days=30) - GET /conversion-metrics [Pro/Premium policy]
4. GetROIMetrics(days=30) - GET /roi-metrics [Pro/Premium policy]
5. GetCustomerAnalysis(days=30) - GET /customer-analysis [Pro/Premium policy]
6. GetPeriodComparison(days=30) - GET /period-comparison [Pro/Premium policy]
7. GetSalesForecast(historicalDays=30, forecastDays=30) - GET /sales-forecast [Pro/Premium policy]
8. GetProductsPerformance(days=30) - GET /products-performance [Pro/Premium policy]
9. ExportAnalytics(ExportRequest) - POST /export [Pro/Premium policy]

**Helper Methods:**
- GetSellerIdFromClaims() - Extracts seller_id from JWT claims

**Authorization Policies:**
- "SellerProPremium" - Enforces Pro/Premium plan requirement
- Uses custom SellerPlanRequirement(SellerPlan.Pro)

---

### AnalyticsService
**File:** MarketplaceArtesanato.Services/Services/AnalyticsService.cs
**Lines:** 350
**Dependencies:**
- ArtesianDbContext
- ISettingsService

**Public Methods:**
1. GetPlatformAnalyticsAsync() → PlatformAnalyticsDto
2. GetTopProductsAsync(limit=10) → List<TopProductDto>
3. GetUserAnalyticsAsync() → UserAnalyticsDto
4. GetSalesByPeriodAsync() → List<SalesPeriodDto>
5. GetCategoryDistributionAsync() → List<CategoryDistributionDto>
6. GetPlatformHealthAsync() → PlatformHealthDto
7. GetSellerPerformanceAsync() → List<CommissionReportItemResponse>
8. GetConversionFunnelAsync() → Dictionary<string, int>

**Data Processing Logic:**
- Orders filtered by status (Confirmed, Sent, Delivered)
- GMV calculated from order items
- Commission rates retrieved from ISettingsService
- Platform revenue = (GMV * commission_rate%) + (order_count * service_fee)
- Conversion rate based on unique customers vs total users
- Growth rate calculated YoY

**Performance Notes:**
- Queries use AsNoTracking() for read-only operations
- Includes relationships loaded for complex calculations
- Monthly data aggregation limited to last 12 months

---

### SellerAnalyticsService
**File:** MarketplaceArtesanato.Services/Services/SellerAnalyticsService.cs
**Lines:** 600
**Dependencies:**
- ArtesianDbContext

**Public Methods:**
1. GetAdvancedAnalyticsAsync(sellerId) → AdvancedAnalyticsDto
2. GetPeriodComparisonAsync(sellerId, days=30) → PeriodComparisonDto
3. GetCustomerAnalysisAsync(sellerId) → CustomerAnalysisDto
4. GetProductPerformanceAsync(sellerId) → List<ProductPerformanceDto>
5. GetTrendsAsync(sellerId, days=90) → List<TrendDataDto>
6. GetHourlyRevenueDistributionAsync(sellerId) → List<HourlyRevenueDto>
7. GetCouponEffectivenessAsync(sellerId) → CouponEffectivenessDto
8. GetAIInsightsAsync(sellerId) → AIInsightsDto [Premium only]
9. GetRevenueForecastAsync(sellerId, daysAhead=30) → RevenueForecastDto [Premium only]
10. GetCustomerSegmentationAsync(sellerId) → CustomerSegmentationDto [Premium only]
11. GetSeasonalAnalysisAsync(sellerId) → SeasonalAnalysisDto [Premium only]
12. ExportAnalyticsAsCSVAsync(sellerId) → byte[] [Premium only]
13. ExportAnalyticsAsPDFAsync(sellerId) → byte[] [Premium only]

**Authorization:**
- All methods check seller subscription plan
- Basic plan: Throws UnauthorizedAccessException
- Premium features require SellerPlan.Premium

**Key Calculations:**
- Revenue: Sum of order items subtotal
- AOV: Total revenue / order count
- Conversion rate: Distinct customers / total views
- Profit margin: ((Price - Cost) / Price) * 100
- Customer retention: (Retained customers / previous period customers) * 100
- Churn rate: (Lost customers / previous period customers) * 100

---

### SellerAnalyticsAdvancedService
**File:** MarketplaceArtesanato.Services/Services/SellerAnalyticsAdvancedService.cs
**Lines:** 688
**Dependencies:**
- ArtesianDbContext
- ILogger<SellerAnalyticsAdvancedService>

**Public Methods:**
1. GetAdvancedDashboardAsync(sellerId, days=30) → AdvancedAnalyticsDashboardDto
2. GetConversionMetricsAsync(sellerId, orders, customers) → ConversionMetricsDto
3. GetROIMetricsAsync(sellerId, orders, products) → ROIMetricsDto
4. GetCustomerCohortAnalysisAsync(sellerId, customers, orders) → CustomerCohortAnalysisDto
5. GetPeriodComparisonAsync(sellerId, days=30) → PeriodComparisonAdvancedDto
6. GenerateSalesForecastAsync(sellerId, historicalDays=30, forecastDays=30) → SalesForecatDto
7. GetLifetimeValueAnalysisAsync(sellerId, customers, orders) → LifetimeValueAnalysisDto
8. GenerateExportAsync(sellerId, periodStart, periodEnd, format="PDF") → AnalyticsExportDto

**Private Helper Methods:**
- ValidateSellerHasAdvancedAccess(sellerId)
- GetSellerOrdersAsync(sellerId, startDate, endDate)
- GetCustomersAsync(sellerId, startDate, endDate)
- CalculateTotalProfit(orders, products)
- GetHourlyConversionData(orders)
- CalculateConversionTrendAsync(sellerId, days)
- CalculateROITrendAsync(sellerId, days)
- GetTopProductsByROI(orders, products, topCount)
- CreatePeriodMetric(current, previous)
- GetDailyComparisonAsync(sellerId, currentStart, currentEnd, days)
- MapProductPerformance(orders, products)
- GetCategoryPerformance(orders, products)
- GetCustomerCohorts(customers, orders)
- CreateLTVSegments(customerLTVs)

**Data Models Returned:**
- Complete dashboard combining all metrics
- Hourly conversion breakdown (0-23 hours)
- Daily trend comparisons
- Product ROI rankings
- Customer cohort analysis
- Sales forecast with confidence bounds

**Forecasting Algorithm:**
- Uses 7-day moving average
- Confidence: 75%
- Forecast range: ±10% bounds
- Trend detection based on last vs 2nd-to-last day

---

## Data Flow Diagram

\\\
Frontend
   ↓
[Analytics Events] → POST /api/analytics/events
   ↓
AnalyticsController.LogAnalyticsEvents()
   ├─→ Validation (max 100 events)
   ├─→ Capture context (User-Agent, IP, timestamp)
   └─→ Debug.WriteLine() [No persistence]

Dashboard Request
   ↓
[GET /api/sellers/analytics-advanced/dashboard?days=30]
   ↓
SellerAnalyticsAdvancedController
   ├─→ Verify JWT token
   ├─→ Extract sellerId from claims
   ├─→ Check "SellerProPremium" policy
   └─→ SellerAnalyticsAdvancedService.GetAdvancedDashboardAsync(sellerId, 30)
       ├─→ ValidateSellerHasAdvancedAccess()
       ├─→ GetSellerOrdersAsync(sellerId, -30d, now)
       ├─→ GetCustomersAsync(sellerId, -30d, now)
       ├─→ GetProducts(sellerId)
       └─→ Parallel calculations:
           ├─→ GetConversionMetricsAsync()
           ├─→ GetROIMetricsAsync()
           ├─→ GetCustomerCohortAnalysisAsync()
           ├─→ GetPeriodComparisonAsync()
           ├─→ GenerateSalesForecastAsync()
           ├─→ GetLifetimeValueAnalysisAsync()
           ├─→ MapProductPerformance()
           └─→ GetCategoryPerformance()
       
       Return AdvancedAnalyticsDashboardDto
   ↓
Response (JSON)
   ↓
Frontend renders visualizations
\\\

---

## Database Queries Patterns

### 1. Get Seller Orders in Period
\\\csharp
var orders = await _context.Orders
    .AsNoTracking()
    .Include(o => o.Items)
    .Include(o => o.Buyer)
    .Where(o => o.Items.Any(i => i.SellerId == sellerId) 
            && o.CreatedAt >= startDate 
            && o.CreatedAt <= endDate 
            && !o.IsDeleted)
    .ToListAsync();
\\\

### 2. Calculate Revenue by Seller
\\\csharp
var revenue = orders.SelectMany(o => o.Items)
    .Where(i => i.SellerId == sellerId)
    .Sum(i => i.Subtotal);
\\\

### 3. Get Top Products by Sales
\\\csharp
var topProducts = await _context.OrderItems
    .AsNoTracking()
    .GroupBy(oi => oi.Product.Id)
    .OrderByDescending(g => g.Sum(x => x.UnitPrice * x.Quantity))
    .Take(10)
    .Select(g => new { ... })
    .ToListAsync();
\\\

### 4. Calculate Conversion Rate
\\\csharp
var uniqueCustomers = orders
    .Select(o => o.BuyerId)
    .Distinct()
    .Count();

var conversionRate = totalUsers > 0 
    ? ((decimal)uniqueCustomers / totalUsers) * 100 
    : 0;
\\\

---

## DTO Hierarchy

### Request DTOs
\\\
AnalyticsEventDto
├─ eventName: string
├─ eventCategory: string
├─ eventLabel: string
├─ eventValue: decimal?
├─ customData: Dictionary
├─ timestamp: DateTime
├─ userAgent: string
├─ ipAddress: string
└─ userId: string

AnalyticsEventBatchDto
├─ events: List<AnalyticsEventDto>
└─ batchTimestamp: DateTime?

ExportRequest
├─ periodStart: DateTime
├─ periodEnd: DateTime
└─ format: string
\\\

### Response DTOs
\\\
PlatformAnalyticsDto
├─ totalGMV
├─ totalOrders
├─ totalUsers
├─ totalSellers
├─ totalProducts
├─ platformRevenue
├─ averageOrderValue
├─ conversionRate
├─ newUsersThisMonth
├─ newOrdersThisMonth
└─ growthRate

AdvancedAnalyticsDashboardDto
├─ sellerId
├─ sellerName
├─ generatedAt
├─ Summary metrics (revenue, profit, orders, customers, aov)
├─ conversionMetrics: ConversionMetricsDto
├─ roiMetrics: ROIMetricsDto
├─ customerAnalysis: CustomerCohortAnalysisDto
├─ periodComparison: PeriodComparisonAdvancedDto
├─ salesForecast: SalesForecatDto
├─ lifetimeValueAnalysis: LifetimeValueAnalysisDto
├─ topProducts: List<ProductPerformanceAdvancedDto>
└─ categoryPerformance: List<CategoryPerformanceDto>

ConversionMetricsDto
├─ conversionRate
├─ clickCount
├─ purchaseCount
├─ abandonedCarts
├─ cartAbandonmentRate
├─ conversionChangePercent
└─ hourlyData: List<HourlyConversionDto>

ROIMetricsDto
├─ totalInvestment
├─ totalReturn
├─ roiPercent
├─ netProfit
├─ profitMargin
├─ roiPeriodChange
└─ topProductsByROI: List<ProductROIDto>
\\\

---

## Authorization Flow

\\\
HTTP Request with JWT
   ↓
[Authorization] attribute validates token exists
   ↓
Roles = "Seller" ✓ Check JWT 'role' claim
   ↓
Policy = "SellerProPremium" ✓ Custom authorization handler
   ├─→ Check 'role' claim == "Seller"
   ├─→ Check 'seller_plan' claim == "Pro" OR "Premium"
   └─→ Service method validates seller exists and not deleted
   ↓
Access Granted → Proceed to endpoint
OR
Access Denied → Return 403 Forbidden
\\\

---

## Testing Scenarios

### Happy Path: Seller Views Dashboard
1. Seller logs in (gets JWT with seller_id, seller_plan=Pro)
2. Frontend GET /api/sellers/analytics-advanced/dashboard?days=30
3. Controller extracts sellerId from claims
4. Policy validates Pro plan
5. Service fetches orders from past 30 days
6. Calculates all metrics
7. Returns complete AdvancedAnalyticsDashboardDto
8. Frontend renders 10+ visualizations

### Error Path: Basic Plan Seller Tries Advanced
1. Seller logs in (gets JWT with seller_plan=Basic)
2. Frontend GET /api/sellers/analytics-advanced/dashboard?days=30
3. Policy validation FAILS
4. Returns 403 Forbidden
5. Frontend shows upgrade prompt

### Error Path: Event Batch Too Large
1. Frontend POST /api/analytics/events
2. Body contains 150 events (> limit of 100)
3. Controller validation fails
4. Returns 400 Bad Request
   \\\json
   { "message": "Too many events in single request", "limit": 100, "received": 150 }
   \\\

---

## Performance Metrics

### Query Performance (Estimated)
- GetAdvancedDashboard: 2-3 queries, ~500ms
- GetPlatformAnalytics: 8-10 queries, ~1000ms
- GetTopProducts(limit=10): 1 query, ~100ms
- GetConversionFunnel: 4 queries, ~400ms

### Response Payload Sizes
- Dashboard (full): ~50-100 KB
- Platform analytics: ~1-2 KB
- Top products: ~5-10 KB
- Export DTO: ~200-500 KB

### Database Indexes Recommended
- Orders.CreatedAt
- Orders.BuyerId
- OrderItems.SellerId
- Products.SellerId
- Products.StockQuantity

---

## Code Quality Notes

### Strengths
✓ Clear separation of concerns (Controller → Service → Data)
✓ Async/await throughout
✓ Comprehensive validation
✓ Plan-based authorization enforcement
✓ Detailed logging in advanced service
✓ No tracking overhead (stateless calculations)

### Areas for Improvement
⚠ Event persistence not implemented (logging only)
⚠ No caching layer (recalculates every request)
⚠ Hard-coded values (20% product cost, 70% investment estimate)
⚠ Limited error handling (generic 500 errors)
⚠ No rate limiting
⚠ Missing request/response compression
⚠ Export file generation incomplete (headers only, no PDF/CSV lib)

---

## Integration Points with Other Services

1. **ISettingsService**
   - GetServiceFeeAsync()
   - GetCommissionRateAsync()

2. **ArtesianDbContext**
   - Orders, OrderItems
   - Products, ProductCategory
   - Users, Sellers
   - Carts, CartItems
   - Coupons, CouponUsages
   - Subscriptions

3. **Authentication/Authorization**
   - JWT token parsing
   - Custom policy handler (SellerProPremium)
   - Claims: seller_id, seller_plan, role

4. **Logging**
   - ILogger<T> in AdvancedService
   - Debug.WriteLine in Controller

