# ANALYTICS API - QUICK REFERENCE GUIDE

## API Endpoints Summary

### 1. EVENT TRACKING (Public, No Auth Required)
POST /api/analytics/events
├─ Accepts: AnalyticsEventBatchDto (max 100 events)
├─ Returns: { message, count, timestamp }
└─ Access: AllowAnonymous

### 2. ADMIN ANALYTICS (Admin Role Required)
GET /api/analytics/platform           → PlatformAnalyticsDto
GET /api/analytics/top-products       → List<TopProductDto> (limit: 10)
GET /api/analytics/users              → UserAnalyticsDto
GET /api/analytics/sales-period       → List<SalesPeriodDto> (last 12 months)
GET /api/analytics/category-distribution → List<CategoryDistributionDto>
GET /api/analytics/health             → PlatformHealthDto
GET /api/analytics/sellers            → List<CommissionReportItemResponse>
GET /api/analytics/conversion-funnel  → Dictionary<string, int>

### 3. SELLER ANALYTICS (Seller Role + Pro/Premium Plan Required)
GET /api/sellers/analytics-advanced/check-access
   └─ Returns: { hasAccess, plan, message, upgradeUrl }

GET /api/sellers/analytics-advanced/dashboard?days=30
   └─ Returns: AdvancedAnalyticsDashboardDto (comprehensive dashboard)

GET /api/sellers/analytics-advanced/conversion-metrics?days=30
   └─ Returns: ConversionMetricsDto { rate, clicks, purchases, abandoned }

GET /api/sellers/analytics-advanced/roi-metrics?days=30
   └─ Returns: ROIMetricsDto { investment, return, profit, margin, topProducts }

GET /api/sellers/analytics-advanced/customer-analysis?days=30
   └─ Returns: { cohortAnalysis, lifetimeValue }

GET /api/sellers/analytics-advanced/period-comparison?days=30
   └─ Returns: PeriodComparisonAdvancedDto { revenue, orders, customers, aov }

GET /api/sellers/analytics-advanced/sales-forecast?historicalDays=30&forecastDays=30
   └─ Returns: SalesForecatDto { points, trend, expectedRevenue, confidence }

GET /api/sellers/analytics-advanced/products-performance?days=30
   └─ Returns: { topProducts[], categoryPerformance[] }

POST /api/sellers/analytics-advanced/export
   ├─ Body: { periodStart, periodEnd, format: 'PDF|CSV|EXCEL' }
   └─ Returns: { export: AnalyticsExportDto, downloadUrl }

---

## FRONTEND INTEGRATION CHECKLIST

### Step 1: Implement Event Tracking
- [ ] Create analytics service/utility
- [ ] Set up event batching (batch every 10 events or 30 seconds)
- [ ] Implement queue for offline events
- [ ] Add privacy consent notice
- [ ] Log key events:
  - [ ] view_item (product page)
  - [ ] add_to_cart (cart action)
  - [ ] view_cart (cart page visit)
  - [ ] begin_checkout (checkout initiated)
  - [ ] purchase (order completed)
  - [ ] search (search query)
  - [ ] login/sign_up (auth events)

### Step 2: Admin Dashboard Integration
- [ ] Fetch platform analytics on dashboard load
- [ ] Create widgets for:
  - [ ] GMV + revenue metrics
  - [ ] User growth chart (monthly)
  - [ ] Top 10 products table
  - [ ] Category distribution pie chart
  - [ ] Conversion funnel visualization
  - [ ] Platform health score gauge
- [ ] Implement data refresh (auto-refresh every 5 minutes)
- [ ] Add date range filter if needed

### Step 3: Seller Dashboard Integration
- [ ] Check seller plan access (Pro/Premium required)
- [ ] Show upgrade prompt if Basic plan
- [ ] Fetch advanced dashboard with configurable period
- [ ] Create visualizations:
  - [ ] Revenue trend chart (30/90 day)
  - [ ] Conversion rate metrics
  - [ ] ROI breakdown by product
  - [ ] Customer cohort table
  - [ ] Sales forecast chart
  - [ ] Top products performance
- [ ] Add export functionality (PDF/CSV)
- [ ] Implement period comparison toggle

### Step 4: Performance Optimization
- [ ] Add request caching (5-10 min TTL)
- [ ] Implement lazy loading for charts
- [ ] Use pagination for large datasets
- [ ] Debounce filter/date changes (300ms)
- [ ] Preload next period's data

---

## KEY METRICS BY USE CASE

### Understanding GMV vs Revenue
- **GMV (Gross Merchandise Value):** Total value of all orders
- **Platform Revenue:** Commission + service fees (calculated separately)

### Conversion Metrics Explained
| Metric | Formula | Interpretation |
|--------|---------|-----------------|
| Conversion Rate | (Orders / Visitors) * 100 | % of visitors who purchase |
| Abandonment Rate | (Abandoned Carts / Cart Users) * 100 | % of carts not checked out |
| AOV | Total Revenue / Total Orders | Average $ per order |

### Trend Indicators
- **Green Up Arrow:** Metric trending up YoY
- **Red Down Arrow:** Metric trending down YoY
- **Percentage:** % change from previous period

### ROI Calculation
- Investment: Estimated product cost (20% of price)
- Return: Actual revenue
- ROI%: ((Return - Investment) / Investment) * 100

### Customer Segments (LTV-Based)
- **High Value:** LTV > Median * 1.5
- **Medium Value:** Median * 0.5 ≤ LTV ≤ Median * 1.5
- **Low Value:** LTV < Median * 0.5

---

## COMMON IMPLEMENTATION PATTERNS

### Pattern 1: Analytics Event Batching
\\\javascript
class AnalyticsService {
  constructor() {
    this.queue = [];
    this.batchSize = 10;
    this.flushInterval = 30000; // 30 seconds
    this.startFlushTimer();
  }

  trackEvent(eventName, category, label, data) {
    const event = {
      eventName, eventCategory: category, eventLabel: label,
      customData: data, timestamp: new Date().toISOString()
    };
    
    this.queue.push(event);
    if (this.queue.length >= this.batchSize) {
      this.flush();
    }
  }

  async flush() {
    if (this.queue.length === 0) return;
    
    const batch = { events: this.queue };
    this.queue = [];
    
    await fetch('/api/analytics/events', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(batch),
      keepalive: true // Important for page unload
    });
  }

  startFlushTimer() {
    setInterval(() => this.flush(), this.flushInterval);
  }
}
\\\

### Pattern 2: Dashboard Data Fetching with Cache
\\\javascript
class AnalyticsCache {
  constructor(ttl = 300000) { // 5 minutes default
    this.cache = new Map();
    this.ttl = ttl;
  }

  async get(key, fetchFn) {
    const cached = this.cache.get(key);
    const now = Date.now();
    
    if (cached && (now - cached.timestamp) < this.ttl) {
      return cached.data;
    }
    
    const data = await fetchFn();
    this.cache.set(key, { data, timestamp: now });
    return data;
  }

  clear(key) {
    if (key) this.cache.delete(key);
    else this.cache.clear();
  }
}

// Usage
const cache = new AnalyticsCache(300000);

async function getDashboard(sellerId, days = 30) {
  return cache.get(
    \dashboard-\-\\,
    () => fetch(\/api/sellers/analytics-advanced/dashboard?days=\\, {
      headers: { 'Authorization': \Bearer \\ }
    }).then(r => r.json())
  );
}
\\\

### Pattern 3: Error Handling
\\\javascript
async function fetchAnalytics(endpoint, options = {}) {
  try {
    const response = await fetch(endpoint, {
      headers: {
        'Authorization': \Bearer \\,
        'Content-Type': 'application/json',
        ...options.headers
      }
    });

    if (!response.ok) {
      if (response.status === 401) {
        // Handle auth error - redirect to login
        redirectToLogin();
      } else if (response.status === 403) {
        // Handle access denied - show upgrade prompt
        showUpgradePrompt();
      } else if (response.status === 404) {
        // Handle not found
        throw new Error('Resource not found');
      }
    }

    return await response.json();
  } catch (error) {
    console.error('Analytics fetch failed:', error);
    showErrorNotification('Failed to load analytics');
    throw error;
  }
}
\\\

---

## AUTHENTICATION TEMPLATE

### JWT Token Structure (Expected Claims)
\\\json
{
  "nameid": "user-id-uuid",
  "seller_id": "seller-id-uuid",
  "seller_plan": "Pro|Premium|Basic",
  "role": "Admin|Seller|Customer",
  "email": "user@example.com",
  "exp": 1705334400
}
\\\

### Adding Authorization Header
\\\javascript
const headers = {
  'Authorization': \Bearer \\,
  'Content-Type': 'application/json'
};
\\\

---

## DASHBOARD LAYOUT RECOMMENDATIONS

### Admin Analytics Dashboard
Layout: 2-3 column grid
1. KPI Cards (Revenue, Orders, Users, Health Score)
2. Charts Row 1: Revenue Trend (line), Top Products (bar)
3. Charts Row 2: Category Distribution (pie), Funnel (funnel)
4. Tables: Top 10 products, Seller performance

### Seller Analytics Dashboard
Layout: Tabbed interface
Tab 1: Overview
├─ KPI Cards: Revenue, Orders, Customers, AOV
├─ Revenue Trend Chart
└─ Top 3 Products

Tab 2: Advanced Metrics
├─ Conversion Analysis
├─ ROI Breakdown
├─ Customer Segments
└─ Forecast Chart

Tab 3: Period Comparison
├─ Side-by-side metrics
├─ Trend indicators
└─ Daily comparison chart

Tab 4: Export
├─ Date range picker
├─ Format selector (PDF/CSV/Excel)
└─ Download button

---

## TESTING CHECKLIST

### Unit Tests
- [ ] Event validation (max 100 events)
- [ ] Plan-based authorization
- [ ] Date range calculations
- [ ] ROI calculations

### Integration Tests
- [ ] Event posting flow
- [ ] Admin dashboard data accuracy
- [ ] Seller plan access control
- [ ] Export generation

### E2E Tests
- [ ] Complete analytics flow from event to dashboard
- [ ] Plan upgrade prompts
- [ ] Export download
- [ ] Period comparison changes

### Performance Tests
- [ ] Dashboard load time < 2 seconds
- [ ] Event submission < 500ms
- [ ] Export generation < 5 seconds

---

## NEXT STEPS FOR COMPLETION

1. **Database Persistence**
   - Create AnalyticsEvent table
   - Implement event storage instead of console logging

2. **View Tracking**
   - Add product view counter to Products table
   - Update on each product view

3. **Export Enhancement**
   - Integrate PDF generation library (iTextSharp)
   - Implement CSV export with proper formatting

4. **Premium Features**
   - Implement AI insights generation
   - Add seasonal analysis endpoint
   - Create anomaly detection

5. **Real-time Updates**
   - Implement SignalR for live metrics
   - Add WebSocket support for streaming data

6. **Additional Metrics**
   - Customer acquisition cost (CAC)
   - Return on ad spend (ROAS)
   - Churn prediction
   - Customer lifetime value (CLV) refinement

