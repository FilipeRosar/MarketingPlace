import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AdvancedAnalyticsDashboard {
  totalRevenue: number;
  totalProfit: number;
  totalOrders: number;
  totalCustomers: number;
  averageOrderValue: number;
  conversionRate: number;
  generatedAt: string;

  conversionMetrics: ConversionMetrics;
  roiMetrics: ROIMetrics;
  customerCohortAnalysis: CustomerCohortAnalysis;
  periodComparison: PeriodComparison;
  salesForecast: SalesForecast;
  lifetimeValueAnalysis: LifetimeValueAnalysis;

  topProducts: ProductPerformance[];
  categoryPerformance: CategoryPerformance[];
}

export interface ConversionMetrics {
  conversionRate: number;
  clickCount: number;
  purchaseCount: number;
  abandonedCarts: number;
  cartAbandonmentRate: number;
  hourlyConversionData: { hour: number; rate: number }[];
}

export interface ROIMetrics {
  totalInvestment: number;
  totalReturn: number;
  roiPercent: number;
  netProfit: number;
  profitMargin: number;
  topProductsByROI: { productId: string; productName: string; roi: number }[];
}

export interface CustomerCohortAnalysis {
  totalCustomers: number;
  newCustomers: number;
  repeatRate: number;
  averageLTV: number;
  retentionRate: number;
  churnRate: number;
  cohorts: Cohort[];
}

export interface Cohort {
  cohortName: string;
  customerCount: number;
  avgValue: number;
  retentionWeek1: number;
  retentionWeek4: number;
}

export interface PeriodComparison {
  currentRevenue: number;
  previousRevenue: number;
  revenueChange: number;
  revenueChangePercent: number;
  currentOrders: number;
  previousOrders: number;
  ordersChange: number;
  currentCustomers: number;
  previousCustomers: number;
  customersChange: number;
  currentAOV: number;
  previousAOV: number;
  aovChange: number;
  currentConversion: number;
  previousConversion: number;
  conversionChange: number;
  dailyComparison: DailyComparison[];
}

export interface DailyComparison {
  date: string;
  currentDayRevenue: number;
  previousDayRevenue: number;
  currentDayOrders: number;
  previousDayOrders: number;
}

export interface SalesForecast {
  forecastStart: string;
  forecastEnd: string;
  expectedRevenue: number;
  confidenceLevel: number;
  forecastPoints: ForecastPoint[];
}

export interface ForecastPoint {
  date: string;
  expectedRevenue: number;
  upperBound: number;
  lowerBound: number;
}

export interface LifetimeValueAnalysis {
  averageLTV: number;
  medianLTV: number;
  maxLTV: number;
  minLTV: number;
  highValueSegment: Segment;
  mediumValueSegment: Segment;
  lowValueSegment: Segment;
  ltvSegments: LTVSegment[];
}

export interface Segment {
  name: string;
  customerCount: number;
  averageLifetimeValue: number;
  churnRate: number;
  purchaseFrequency: number;
}

export interface LTVSegment {
  segmentName: string;
  customerCount: number;
  contributionPercent: number;
  avgLTV: number;
}

export interface ProductPerformance {
  productId: string;
  productName: string;
  category: string;
  price: number;
  salesCount: number;
  revenue: number;
  profit: number;
  profitMargin: number;
  viewCount: number;
  conversionRate: number;
  roiPercent: number;
  rating: number;
  lastSale: string;
}

export interface CategoryPerformance {
  category: string;
  salesCount: number;
  revenue: number;
  contributionPercent: number;
  conversionRate: number;
  averageProductRevenue: number;
}

// Simple metrics for Pro level
export interface SellerMetrics {
  totalRevenue: number;
  totalOrders: number;
  totalCustomers: number;
  averageOrderValue: number;
  conversionRate: number;
  topProducts: ProductPerformance[];
}

export interface TrendData {
  date: string;
  revenue: number;
  orders: number;
}

@Injectable({
  providedIn: 'root'
})
export class SellerAnalyticsService {
  private apiUrl = `${environment.apiUrl}/sellers/analytics-advanced`;

  constructor(private http: HttpClient) { }

  /**
   * Get complete advanced analytics dashboard (Pro/Premium only)
   */
  getAdvancedDashboard(days: number = 30): Observable<AdvancedAnalyticsDashboard> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<AdvancedAnalyticsDashboard>(
      `${this.apiUrl}/dashboard`,
      { params }
    );
  }

  /**
   * Get conversion metrics
   */
  getConversionMetrics(): Observable<ConversionMetrics> {
    return this.http.get<ConversionMetrics>(`${this.apiUrl}/conversion-metrics`);
  }

  /**
   * Get ROI metrics
   */
  getROIMetrics(): Observable<ROIMetrics> {
    return this.http.get<ROIMetrics>(`${this.apiUrl}/roi-metrics`);
  }

  /**
   * Get customer analysis
   */
  getCustomerAnalysis(): Observable<CustomerCohortAnalysis> {
    return this.http.get<CustomerCohortAnalysis>(`${this.apiUrl}/customer-analysis`);
  }

  /**
   * Get period comparison (WoW, MoM)
   */
  getPeriodComparison(days: number = 30): Observable<PeriodComparison> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<PeriodComparison>(
      `${this.apiUrl}/period-comparison`,
      { params }
    );
  }

  /**
   * Get sales forecast (Premium only)
   */
  getSalesForecast(forecastDays: number = 30): Observable<SalesForecast> {
    const params = new HttpParams().set('forecastDays', forecastDays.toString());
    return this.http.get<SalesForecast>(
      `${this.apiUrl}/sales-forecast`,
      { params }
    );
  }

  /**
   * Get customer segmentation (Premium only)
   */
  getCustomerSegmentation(): Observable<LifetimeValueAnalysis> {
    return this.http.get<LifetimeValueAnalysis>(`${this.apiUrl}/customer-segmentation`);
  }

  /**
   * Get products performance
   */
  getProductsPerformance(): Observable<ProductPerformance[]> {
    return this.http.get<ProductPerformance[]>(`${this.apiUrl}/products-performance`);
  }

  /**
   * Export analytics (Premium only)
   */
  exportAnalytics(format: 'csv' | 'pdf' | 'xlsx', periodStart: string, periodEnd: string): Observable<Blob> {
    const params = new HttpParams()
      .set('format', format)
      .set('periodStart', periodStart)
      .set('periodEnd', periodEnd);

    return this.http.get(
      `${this.apiUrl}/export`,
      { params, responseType: 'blob' }
    );
  }

  /**
   * Check seller access to advanced analytics
   */
  checkAccess(): Observable<{ succeeded: boolean; message: string }> {
    return this.http.get<{ succeeded: boolean; message: string }>(`${this.apiUrl}/check-access`);
  }
}
