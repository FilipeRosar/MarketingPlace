# ANALYTICS API - COMPREHENSIVE TECHNICAL SPECIFICATION

## Overview
The Marketplace Artesanato backend provides multiple layers of analytics:
1. **Platform Analytics** - Admin-only dashboard for platform health
2. **Seller Analytics** - Basic metrics for sellers (Pro/Premium plans required for advanced features)
3. **Seller Advanced Analytics** - Detailed metrics with predictions and AI insights (Pro/Premium plans)
4. **Frontend Analytics Events** - Anonymous tracking of user interactions

---

## 1. FRONTEND ANALYTICS EVENTS ENDPOINT

### Endpoint: POST /api/analytics/events
**Access:** Public (AllowAnonymous)
**Rate Limit:** Max 100 events per request

#### Request Body
\\\json
{
  "events": [
    {
      "eventName": "view_item",
      "eventCategory": "Product",
      "eventLabel": "Product ID or name",
      "eventValue": 49.99,
      "customData": {
        "productId": "uuid",
        "productName": "Item Name",
        "category": "Ceramics",
        "price": 49.99,
        "userId": "user-uuid (optional)"
      },
      "timestamp": "2024-01-15T10:30:00Z",
      "userAgent": "Mozilla/5.0...",
      "ipAddress": "192.168.1.1",
      "userId": "user-uuid (optional)"
    }
  ],
  "batchTimestamp": "2024-01-15T10:35:00Z"
}
\\\

#### Supported Event Types
**Product Events:**
- \iew_item\ - User viewed a product
- \item_list_view\ - User viewed category/search results
- \dd_to_cart\ - Item added to cart
- \emove_from_cart\ - Item removed from cart
- \dd_to_wishlist\ - Item wishlisted
- \emove_from_wishlist\ - Removed from wishlist
- \iew_cart\ - Cart page viewed
- \iew_item_details\ - Detailed product page viewed

**Checkout Events:**
- \egin_checkout\ - Checkout initiated
- \dd_shipping_info\ - Shipping address entered
- \dd_payment_info\ - Payment method selected
- \purchase\ - Order completed
- \pply_coupon\ - Discount code applied
- \emove_coupon\ - Discount code removed

**Search Events:**
- \search\ - Search query entered
- \iew_search_results\ - Search results displayed

**Auth Events:**
- \login\ - User logged in
- \sign_up\ - New account created
- \logout\ - User logged out

**Navigation Events:**
- \page_view\ - Page loaded
- \screen_view\ - App screen loaded

**Error Events:**
- \rror\ - Client error
- \xception\ - Exception thrown
- \pi_error\ - API request failed

#### Response (200 OK)
\\\json
{
  "message": "Events received successfully",
  "count": 5,
  "timestamp": "2024-01-15T10:35:00Z"
}
\\\

#### Error Responses
**400 Bad Request:**
\\\json
{
  "message": "No events provided"
}
\\\

\\\json
{
  "message": "Too many events in single request",
  "limit": 100,
  "received": 150
}
\\\

**500 Internal Server Error:**
\\\json
{
  "message": "Error processing analytics events"
}
\\\

#### Implementation Notes
- Events are logged to console (Debug output)
- User-Agent and IP Address are captured from request context
- Timestamp is set server-side to UTC now
- No persistence layer configured yet (only debug logging)

---

## 2. PLATFORM ANALYTICS ENDPOINTS (Admin Only)

### Base Route: /api/analytics
**Authentication:** Required (Bearer Token)
**Authorization:** Roles = "Admin"

---

### 2.1 GET /api/analytics/platform
**Description:** Get overall platform analytics

#### Response (200 OK)
\\\json
{
  "totalGMV": 150000.50,
  "totalOrders": 450,
  "totalUsers": 1200,
  "totalSellers": 85,
  "totalProducts": 3200,
  "platformRevenue": 15500.25,
  "averageOrderValue": 333.33,
  "conversionRate": 37.5,
  "newUsersThisMonth": 120,
  "newOrdersThisMonth": 45,
  "growthRate": 12.5
}
\\\

**Metrics:**
- **totalGMV**: Gross Merchandise Value (sum of all confirmed orders)
- **platformRevenue**: Commission + service fees collected
- **conversionRate**: (Unique Customers / Total Users) * 100
- **growthRate**: ((Current Month Orders - Previous Month Orders) / Previous Month Orders) * 100

---

### 2.2 GET /api/analytics/top-products
**Description:** Get best performing products by sales

#### Query Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| limit | int | 10 | Number of products to return (1-100) |

#### Response (200 OK)
\\\json
[
  {
    "productId": "uuid",
    "productName": "Handmade Ceramic Vase",
    "sellerName": "Artisan Studio",
    "totalSales": 5000.00,
    "totalQuantitySold": 25,
    "averageRating": 4.8,
    "totalReviews": 42
  },
  ...
]
\\\

---

### 2.3 GET /api/analytics/users
**Description:** User population statistics

#### Response (200 OK)
\\\json
{
  "totalUsers": 1200,
  "buyers": 950,
  "sellers": 85,
  "admins": 3,
  "newUsersThisMonth": 120,
  "activeUsersThisMonth": 450,
  "averageUserLifetimeValue": 157.89
}
\\\

---

### 2.4 GET /api/analytics/sales-period
**Description:** Monthly sales data for last 12 months

#### Response (200 OK)
\\\json
[
  {
    "period": "2024-01",
    "totalSales": 15000.50,
    "totalOrders": 45,
    "averageOrderValue": 333.33,
    "newCustomers": 25
  },
  ...
]
\\\

---

### 2.5 GET /api/analytics/category-distribution
**Description:** Product distribution and sales by category

#### Response (200 OK)
\\\json
[
  {
    "categoryId": "uuid",
    "categoryName": "Ceramics",
    "productCount": 450,
    "totalSales": 45000.00,
    "percentage": 30.0
  },
  ...
]
\\\

---

### 2.6 GET /api/analytics/health
**Description:** Platform health and operational metrics

#### Response (200 OK)
\\\json
{
  "pendingSellers": 5,
  "pendingOrders": 12,
  "lowStockProducts": 28,
  "inactiveListings": 150,
  "platformHealthScore": 85.5
}
\\\

**Scoring:**
- Health Score (0-100) based on:
  - Pending sellers (15% weight)
  - Pending orders (30% weight)
  - Low stock products (25% weight)
  - Inactive listings (30% weight)

---

### 2.7 GET /api/analytics/sellers
**Description:** Seller performance metrics (commission report)

#### Response (200 OK)
\\\json
[
  {
    "sellerId": "uuid",
    "sellerName": "Artisan Studio",
    "totalSales": 15000.00,
    "commissionEarned": 750.00,
    "rate": 5.0
  },
  ...
]
\\\

---

### 2.8 GET /api/analytics/conversion-funnel
**Description:** User conversion funnel (visitors → cart → checkout → completion)

#### Response (200 OK)
\\\json
{
  "Visitors": 2500,
  "Cart": 750,
  "CheckedOut": 125,
  "Completed": 85
}
\\\

---

## 3. SELLER ANALYTICS ENDPOINTS (Seller Role Required)

### Base Route: /api/sellers/analytics-advanced
**Authentication:** Required (Bearer Token)
**Authorization:** Roles = "Seller" + Policy = "SellerProPremium"

**Note:** All endpoints except check-access require Pro or Premium plan

---

### 3.1 GET /api/sellers/analytics-advanced/check-access
**Authentication:** Required
**Authorization:** Roles = "Seller"

**Description:** Check if seller has access to advanced analytics

#### Response (200 OK - Has Access)
\\\json
{
  "hasAccess": true,
  "plan": "Pro",
  "message": "Você tem acesso a analytics avançado"
}
\\\

#### Response (200 OK - No Access)
\\\json
{
  "hasAccess": false,
  "message": "Você não tem plano Pro ou Premium. Faça upgrade para acessar analytics avançado.",
  "upgradeUrl": "/sellers/subscription/upgrade"
}
\\\

---

### 3.2 GET /api/sellers/analytics-advanced/dashboard
**Description:** Complete advanced analytics dashboard

#### Query Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| days | int | 30 | Analysis period (1-365 days) |

#### Response (200 OK)
\\\json
{
  "sellerId": "uuid",
  "sellerName": "Artisan Studio",
  "generatedAt": "2024-01-15T10:30:00Z",
  
  "totalRevenue": 25000.50,
  "totalProfit": 10000.20,
  "totalOrders": 75,
  "totalCustomers": 55,
  "aov": 333.33,
  
  "conversionMetrics": {
    "conversionRate": 36.36,
    "clickCount": 275,
    "purchaseCount": 75,
    "abandonedCarts": 11,
    "cartAbandonmentRate": 20.0,
    "conversionChangePercent": 5.5,
    "hourlyData": [
      {
        "hour": 0,
        "conversionRate": 25.0,
        "clicks": 10,
        "purchases": 2
      },
      ...
    ]
  },
  
  "roiMetrics": {
    "totalInvestment": 17500.35,
    "totalReturn": 25000.50,
    "roiPercent": 42.85,
    "netProfit": 7500.15,
    "profitMargin": 30.0,
    "roiPeriodChange": 2.5,
    "topProductsByROI": [
      {
        "productId": "uuid",
        "productName": "Premium Vase",
        "cost": 25.00,
        "revenue": 500.00,
        "profit": 475.00,
        "roiPercent": 1900.0,
        "unitsSold": 10
      },
      ...
    ]
  },
  
  "customerAnalysis": {
    "totalCustomers": 55,
    "newCustomers": 15,
    "repeatCustomers": 40,
    "repeatCustomerRate": 72.73,
    "averageCustomerLTV": 454.55,
    "customerRetentionRate": 72.73,
    "churnedCustomers": 0,
    "churnRate": 0.0,
    "cohorts": [
      {
        "cohortName": "Últimos 30 dias",
        "customersCount": 15,
        "totalSpent": 6000.00,
        "averageLTV": 400.00,
        "retentionRate": 75.0,
        "churnRate": 25.0
      },
      ...
    ]
  },
  
  "periodComparison": {
    "currentPeriod": {
      "startDate": "2023-12-16T00:00:00Z",
      "endDate": "2024-01-15T00:00:00Z"
    },
    "previousPeriod": {
      "startDate": "2023-11-16T00:00:00Z",
      "endDate": "2023-12-16T00:00:00Z"
    },
    "revenue": {
      "current": 25000.50,
      "previous": 23500.00,
      "changeAmount": 1500.50,
      "changePercent": 6.38,
      "isTrendingUp": true
    },
    "orders": {
      "current": 75,
      "previous": 70,
      "changeAmount": 5,
      "changePercent": 7.14,
      "isTrendingUp": true
    },
    "customers": {
      "current": 55,
      "previous": 50,
      "changeAmount": 5,
      "changePercent": 10.0,
      "isTrendingUp": true
    },
    "aov": {
      "current": 333.33,
      "previous": 335.71,
      "changeAmount": -2.38,
      "changePercent": -0.71,
      "isTrendingUp": false
    },
    "conversionRate": {
      "current": 1.36,
      "previous": 1.40,
      "changeAmount": -0.04,
      "changePercent": -2.86,
      "isTrendingUp": false
    },
    "dailyComparison": [
      {
        "date": "2024-01-01T00:00:00Z",
        "currentRevenue": 800.00,
        "previousRevenue": 750.00,
        "currentOrders": 2,
        "previousOrders": 2
      },
      ...
    ]
  },
  
  "salesForecast": {
    "forecastStart": "2024-01-16T00:00:00Z",
    "forecastEnd": "2024-02-15T00:00:00Z",
    "expectedRevenue": 24000.00,
    "confidence": 0.75,
    "trend": "Crescente",
    "points": [
      {
        "date": "2024-01-16T00:00:00Z",
        "forecastedRevenue": 800.00,
        "lowBound": 720.00,
        "highBound": 880.00
      },
      ...
    ]
  },
  
  "lifetimeValueAnalysis": {
    "averageLTV": 454.55,
    "medianLTV": 425.00,
    "maxLTV": 2500.00,
    "minLTV": 100.00,
    "highValueCustomers": 12,
    "mediumValueCustomers": 35,
    "lowValueCustomers": 8,
    "segments": [
      {
        "segmentName": "Alto Valor",
        "customerCount": 12,
        "averageLTV": 900.00,
        "totalContribution": 10800.00,
        "contributionPercent": 43.2
      },
      ...
    ]
  },
  
  "topProducts": [
    {
      "productId": "uuid",
      "productName": "Premium Ceramic Vase",
      "category": "Ceramics",
      "price": 49.99,
      "salesCount": 25,
      "revenue": 1249.75,
      "profit": 500.00,
      "profitMargin": 40.0,
      "viewCount": 500,
      "conversionRate": 5.0,
      "roiPercent": 200.0,
      "rating": 5,
      "lastSale": "2024-01-15T08:30:00Z"
    },
    ...
  ],
  
  "categoryPerformance": [
    {
      "categoryName": "Ceramics",
      "productCount": 25,
      "salesCount": 45,
      "revenue": 12500.00,
      "contribution": 50.0,
      "conversionRate": 180.0,
      "averageProductRevenue": 500.00
    },
    ...
  ]
}
\\\

---

### 3.3 GET /api/sellers/analytics-advanced/conversion-metrics
**Description:** Detailed conversion metrics (product views → purchases)

#### Query Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| days | int | 30 | Analysis period (1-365 days) |

#### Response
Returns the conversionMetrics object from the dashboard

---

### 3.4 GET /api/sellers/analytics-advanced/roi-metrics
**Description:** ROI analysis for products and overall business

#### Response
Returns the oiMetrics object from the dashboard

**Key Metrics:**
- Total Investment (estimated product cost)
- Total Return (revenue)
- ROI Percent (profit / investment * 100)
- Profit Margin (profit / revenue * 100)

---

### 3.5 GET /api/sellers/analytics-advanced/customer-analysis
**Description:** Customer cohort and lifetime value analysis

#### Response
\\\json
{
  "cohortAnalysis": { ... },
  "lifetimeValue": { ... }
}
\\\

---

### 3.6 GET /api/sellers/analytics-advanced/period-comparison
**Description:** Compare current period with previous period

#### Query Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| days | int | 30 | Period length (1-365 days) |

#### Response
Returns PeriodComparisonAdvancedDto with:
- Revenue comparison
- Orders comparison
- Customer count comparison
- AOV comparison
- Conversion rate comparison
- Daily trend data

---

### 3.7 GET /api/sellers/analytics-advanced/sales-forecast
**Description:** Sales forecast for upcoming period (simple moving average)

#### Query Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| historicalDays | int | 30 | Days of history to analyze (1-365) |
| forecastDays | int | 30 | Days to forecast (1-365) |

#### Response
\\\json
{
  "forecastStart": "2024-01-16T00:00:00Z",
  "forecastEnd": "2024-02-15T00:00:00Z",
  "expectedRevenue": 24000.00,
  "confidence": 0.75,
  "trend": "Crescente",
  "points": [
    {
      "date": "2024-01-16T00:00:00Z",
      "forecastedRevenue": 800.00,
      "lowBound": 720.00,
      "highBound": 880.00
    }
  ]
}
\\\

**Algorithm:** 7-day moving average with ±10% confidence bounds

---

### 3.8 GET /api/sellers/analytics-advanced/products-performance
**Description:** Top products and category performance

#### Response
\\\json
{
  "topProducts": [ ... ],
  "categoryPerformance": [ ... ]
}
\\\

---

### 3.9 POST /api/sellers/analytics-advanced/export
**Description:** Generate analytics report for export (PDF/CSV/Excel)

#### Request Body
\\\json
{
  "periodStart": "2024-01-01T00:00:00Z",
  "periodEnd": "2024-01-31T23:59:59Z",
  "format": "PDF"
}
\\\

#### Query Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| format | string | false | PDF, CSV, or EXCEL (default: PDF) |

#### Response (200 OK)
\\\json
{
  "message": "Relatório gerado com sucesso em formato PDF",
  "export": {
    "sellerId": "uuid",
    "sellerName": "Artisan Studio",
    "periodStart": "2024-01-01T00:00:00Z",
    "periodEnd": "2024-01-31T23:59:59Z",
    "exportFormat": "PDF",
    "generatedAt": "2024-01-15T10:30:00Z",
    "analytics": { ... },
    "orders": [
      {
        "orderId": "uuid",
        "orderDate": "2024-01-15T10:30:00Z",
        "customerName": "João Silva",
        "orderValue": 500.00,
        "itemCount": 2,
        "status": "Delivered",
        "products": ["Premium Vase", "Ceramic Bowl"]
      }
    ]
  },
  "downloadUrl": "/api/sellers/analytics-advanced/export-download/uuid"
}
\\\

#### Error Responses
**400 Bad Request:**
\\\json
{
  "message": "Data inicial deve ser anterior à data final"
}
\\\

\\\json
{
  "message": "Período máximo é de 365 dias"
}
\\\

\\\json
{
  "message": "Formato deve ser PDF, CSV ou EXCEL"
}
\\\

---

## 4. ERROR HANDLING

### Common HTTP Status Codes
| Status | Scenario |
|--------|----------|
| 200 | Successful request |
| 400 | Bad request (validation error) |
| 401 | Not authenticated |
| 403 | Not authorized (insufficient plan/role) |
| 404 | Resource not found (seller not found) |
| 500 | Internal server error |

### Standard Error Response Format
\\\json
{
  "message": "Error description"
}
\\\

### Plan-Based Access Control
- **Basic Plan Sellers:** Cannot access any advanced analytics
- **Pro Plan Sellers:** Can access all endpoints except premium-only features
- **Premium Plan Sellers:** Can access all endpoints including AI insights and export

### Authorization Policy: "SellerProPremium"
Enforced via custom authorization policy that checks:
1. User role is "Seller"
2. Seller's subscription plan is Pro or Premium
3. Seller is not deleted

---

## 5. AUTHENTICATION REQUIREMENTS

All endpoints except \POST /api/analytics/events\ and \GET /api/sellers/analytics-advanced/check-access\ require:

**Header:**
\\\
Authorization: Bearer {jwt_token}
\\\

**Token Claims (populated from JWT):**
- \seller_id\ (Guid): Seller's unique identifier
- \seller_plan\ (string): Current subscription plan (Basic, Pro, Premium)
- \
ameid\ (string): User ID
- \ole\ (string): User role (Admin, Seller, Customer)

---

## 6. FRONTEND INTEGRATION EXAMPLES

### Example 1: Log Product View
\\\javascript
const event = {
  events: [
    {
      eventName: 'view_item',
      eventCategory: 'Product',
      eventLabel: 'Ceramic Vase #12345',
      eventValue: 49.99,
      customData: {
        productId: '550e8400-e29b-41d4-a716-446655440000',
        productName: 'Handmade Ceramic Vase',
        category: 'Ceramics',
        price: 49.99,
        seller: 'Artisan Studio'
      },
      timestamp: new Date().toISOString(),
      userId: currentUserId
    }
  ]
};

const response = await fetch('/api/analytics/events', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(event)
});
\\\

### Example 2: Log Add to Cart
\\\javascript
const event = {
  events: [
    {
      eventName: 'add_to_cart',
      eventCategory: 'Cart',
      eventLabel: 'Ceramic Vase added to cart',
      eventValue: 49.99,
      customData: {
        productId: '550e8400-e29b-41d4-a716-446655440000',
        quantity: 2,
        cartValue: 99.98
      }
    }
  ]
};

await fetch('/api/analytics/events', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(event)
});
\\\

### Example 3: Fetch Seller Dashboard
\\\javascript
// Check access first
const accessResponse = await fetch(
  '/api/sellers/analytics-advanced/check-access',
  {
    headers: {
      'Authorization': \Bearer \\
    }
  }
);

const accessData = await accessResponse.json();

if (!accessData.hasAccess) {
  console.log('User needs to upgrade to Pro or Premium plan');
  window.location.href = accessData.upgradeUrl;
  return;
}

// Fetch advanced dashboard
const days = 30;
const dashboardResponse = await fetch(
  \/api/sellers/analytics-advanced/dashboard?days=\\,
  {
    headers: {
      'Authorization': \Bearer \\
    }
  }
);

const dashboard = await dashboardResponse.json();

// Display metrics
console.log('Total Revenue:', dashboard.totalRevenue);
console.log('Total Orders:', dashboard.totalOrders);
console.log('Conversion Rate:', dashboard.conversionMetrics.conversionRate);
\\\

### Example 4: Export Analytics
\\\javascript
const exportRequest = {
  periodStart: '2024-01-01T00:00:00Z',
  periodEnd: '2024-01-31T23:59:59Z',
  format: 'PDF'
};

const response = await fetch(
  '/api/sellers/analytics-advanced/export',
  {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': \Bearer \\
    },
    body: JSON.stringify(exportRequest)
  }
);

const result = await response.json();
console.log('Download URL:', result.downloadUrl);
// Open download or trigger file download
window.location.href = result.downloadUrl;
\\\

---

## 7. DATA MODELS REFERENCE

### AnalyticsEventDto
\\\csharp
{
  eventName: string;        // Event type
  eventCategory: string;    // Category
  eventLabel: string;       // Descriptive label
  eventValue: decimal?;     // Numeric value
  customData: object;       // Custom properties
  timestamp: DateTime;      // Event time (UTC)
  userAgent: string;        // Browser UA
  ipAddress: string;        // Client IP
  userId: string;           // User ID (optional)
}
\\\

### AdvancedAnalyticsDashboardDto
Complete dashboard with:
- Summary metrics (revenue, profit, orders, customers)
- Conversion metrics
- ROI metrics
- Customer analysis
- Period comparison
- Sales forecast
- Lifetime value analysis
- Product performance
- Category performance

---

## 8. MISSING/INCOMPLETE IMPLEMENTATIONS

### Known Issues:
1. **Event Persistence:** Events logged to console only (Debug.WriteLine), not persisted to database
2. **View Tracking:** Product view counts always return 0 (no tracking mechanism)
3. **Cart Abandonment:** Estimated at 20% (not calculated from actual data)
4. **Export Downloads:** Export generation works, but file download endpoint not fully implemented
5. **AI Insights:** Basic recommendations only (Premium feature incomplete)
6. **PDF/CSV Generation:** Headers created but no library integration (iTextSharp, CSVHelper)
7. **Seasonal Analysis:** Only available in SellerAnalyticsService (not in advanced controller)
8. **Category Sales:** Category distribution shows 0 for category sales (TODO comment in code)

### Recommendations for Frontend:
1. Implement analytics event batching and periodic flush
2. Add user consent/privacy notice before tracking
3. Use session storage for temporary event buffering
4. Add error handling for failed event submissions
5. Implement caching for dashboard data (30-60 second TTL)
6. Add real-time updates using SignalR or WebSocket for live metrics

---

## 9. PERFORMANCE CONSIDERATIONS

### Query Optimization:
- All queries use \AsNoTracking()\ for read-only operations
- Dashboard queries fetch 30 days of data by default (configurable)
- Top products limited to 10 results
- Daily comparison limited to the analysis period

### Database Indexes Needed:
- Orders.CreatedAt
- Orders.Items.SellerId
- Products.SellerId
- Orders.BuyerId

### Caching Recommendations:
- Dashboard: 5-10 minutes
- Platform analytics: 1 hour (admin view)
- Export data: Generate on-demand

---

## 10. RATE LIMITING

No explicit rate limiting implemented. Recommended:
- Events endpoint: 100 requests/minute per IP
- Admin endpoints: 1000 requests/minute per user
- Seller endpoints: 500 requests/minute per seller

