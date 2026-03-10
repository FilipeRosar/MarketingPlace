import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Coupon {
  id: string;
  code: string;
  description: string;
  type: number;
  discountType: number;
  discountValue: number;
  maxDiscount?: number;
  minOrderValue: number;
  scope: number;
  productId?: string;
  categoryId?: string;
  sellerId?: string;
  platformSharePercentage?: number;
  validFrom: Date;
  validUntil: Date;
  usageLimit: number;
  usageCount: number;
  usageLimitPerUser: number;
  isActive: boolean;
  preventsCombination: boolean;
  onlyWithoutPromotion: boolean;
  onlyFirstPurchase: boolean;
  createdAt: Date;
  updatedAt?: Date;
  creatorSellerId?: string;
  creatorSellerName?: string;
}

export interface CreateCouponRequest {
  code: string;
  description: string;
  type: number;  // 1=Platform, 2=Seller, 3=Intelligent, 4=PlanBased
  discountType: number;  // 1=Percentage, 2=Fixed
  discountValue: number;
  maxDiscount?: number;
  minOrderValue: number;
  scope: number;  // 1=EntireOrder, 2=Product, 3=Category, 4=Seller, 5=WithoutPromotion
  productId?: string;
  categoryId?: string;
  sellerId?: string;
  platformSharePercentage?: number;
  validFrom: Date;
  validUntil: Date;
  usageLimit: number;
  usageLimitPerUser: number;
  isActive: boolean;
  preventsCombination: boolean;
  onlyWithoutPromotion: boolean;
  onlyFirstPurchase: boolean;
}

export interface CouponUsage {
  totalUses: number;
  remainingUses: number;
  totalDiscountGiven: number;
  recentUses: CouponUseDetail[];
}

export interface CouponUseDetail {
  userId: string;
  userName: string;
  orderId: string;
  discountApplied: number;
  usedAt: Date;
}

// Analytics DTOs
export interface CouponROIDto {
  couponId: string;
  couponCode: string;
  totalDiscountGiven: number;
  estimatedRevenueGenerated: number;
  roi: number;
  totalUsages: number;
  averageOrderValue: number;
  conversionRate: number;
  calculatedAt: Date;
}

export interface CouponQuickStatsDto {
  couponId: string;
  code: string;
  discountValue: number;
  usages: number;
  roi: number;
  isActive: boolean;
}

export interface SellerCouponStatsDto {
  sellerId: string;
  activeCoupons: number;
  totalCoupons: number;
  totalDiscountSpent: number;
  totalRevenueGenerated: number;
  averageROI: number;
  conversionRate: number;
  totalCouponUsages: number;
  topCoupons: CouponQuickStatsDto[];
}

export interface DailyPerformanceDto {
  date: Date;
  impressions: number;
  usages: number;
  discountAmount: number;
  orderValue: number;
}

export interface CouponPerformanceDto {
  couponId: string;
  code: string;
  startDate: Date;
  endDate: Date;
  totalImpressions: number;
  totalUsages: number;
  conversionRate: number;
  totalDiscountAmount: number;
  totalOrderValue: number;
  roi: number;
  averageOrderValue: number;
  dailyData: DailyPerformanceDto[];
}

export interface CouponPerformanceComparisonDto {
  couponId: string;
  code: string;
  usages: number;
  roi: number;
  conversionRate: number;
  avgOrderValue: number;
  rank: number;
}

export interface CouponAnalyticsDashboardDto {
  totalSavedByCustomers: number;
  activeCouponsCount: number;
  averageROI: number;
  conversionRate: number;
  topPerformers: CouponPerformanceComparisonDto[];
  bottomPerformers: CouponPerformanceComparisonDto[];
  monthlyTrend: number;
}

export interface CouponAutomationLogDto {
  id: string;
  automationType: string;
  executedAt: Date;
  affectedCoupons: number;
  status: string;
  message: string;
  details?: string;
}

@Injectable({
  providedIn: 'root'
})
export class CouponService {
  private apiUrl = `${environment.apiUrl}/coupons`;

  constructor(private http: HttpClient) { }

  // Admin endpoints
  createCoupon(data: CreateCouponRequest): Observable<Coupon> {
    return this.http.post<Coupon>(this.apiUrl, data);
  }

  getAllCoupons(type?: string, activeOnly?: boolean): Observable<Coupon[]> {
    let url = this.apiUrl;
    const params: any = {};
    if (type) params.type = type;
    if (activeOnly !== undefined) params.activeOnly = activeOnly;
    
    return this.http.get<Coupon[]>(url, { params });
  }

  getCouponById(id: string): Observable<Coupon> {
    return this.http.get<Coupon>(`${this.apiUrl}/${id}`);
  }

  updateCoupon(id: string, data: Partial<CreateCouponRequest>): Observable<Coupon> {
    return this.http.put<Coupon>(`${this.apiUrl}/${id}`, data);
  }

  deleteCoupon(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getCouponUsage(id: string): Observable<CouponUsage> {
    return this.http.get<CouponUsage>(`${this.apiUrl}/${id}/usage`);
  }

  getPlatformCoupons(): Observable<Coupon[]> {
    return this.http.get<Coupon[]>(`${this.apiUrl}/platform/list`);
  }

  // Seller endpoints
  createSellerCoupon(data: CreateCouponRequest): Observable<Coupon> {
    return this.http.post<Coupon>(`${this.apiUrl}/seller`, data);
  }

  getSellerCoupons(): Observable<Coupon[]> {
    return this.http.get<Coupon[]>(`${this.apiUrl}/seller/my-coupons`);
  }

  updateSellerCoupon(id: string, data: Partial<CreateCouponRequest>): Observable<Coupon> {
    return this.http.put<Coupon>(`${this.apiUrl}/seller/${id}`, data);
  }

  deleteSellerCoupon(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/seller/${id}`);
  }

  // Public endpoints
  validateCoupon(code: string, orderTotal: number, productIds: string[] = []): Observable<any> {
    return this.http.post(`${this.apiUrl}/validate`, {
      couponCode: code,
      orderTotal,
      productIds
    });
  }

  applyCoupon(orderId: string, couponCode: string, orderTotal: number, productIds: string[] = []): Observable<any> {
    return this.http.post(`${this.apiUrl}/apply`, {
      orderId,
      couponCode,
      orderTotal,
      productIds
    });
  }

  getActiveCoupons(): Observable<Coupon[]> {
    return this.http.get<Coupon[]>(`${this.apiUrl}/active/list`);
  }

  // ==================== NEW: Analytics Endpoints ====================

  /**
   * Get general statistics for seller's coupons
   */
  getSellerCouponStats(sellerId: string): Observable<SellerCouponStatsDto> {
    return this.http.get<SellerCouponStatsDto>(`${this.apiUrl}/seller/${sellerId}/stats`);
  }

  /**
   * Get ROI calculation for a specific coupon
   */
  getCouponROI(sellerId: string, couponId: string): Observable<CouponROIDto> {
    return this.http.get<CouponROIDto>(
      `${this.apiUrl}/seller/${sellerId}/coupons/${couponId}/analytics`
    );
  }

  /**
   * Get performance data for a coupon within a date range
   */
  getCouponPerformance(
    sellerId: string,
    couponId: string,
    startDate: Date,
    endDate: Date
  ): Observable<CouponPerformanceDto> {
    const params = {
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString()
    };
    return this.http.get<CouponPerformanceDto>(
      `${this.apiUrl}/seller/${sellerId}/coupons/${couponId}/performance`,
      { params }
    );
  }

  /**
   * Compare performance between seller's coupons
   */
  getSellerCouponsComparison(sellerId: string, topN: number = 10): Observable<CouponPerformanceComparisonDto[]> {
    const params = { topN: topN.toString() };
    return this.http.get<CouponPerformanceComparisonDto[]>(
      `${this.apiUrl}/seller/${sellerId}/coupons/comparison`,
      { params }
    );
  }

  /**
   * Get complete analytics dashboard for seller
   */
  getSellerAnalyticsDashboard(sellerId: string): Observable<CouponAnalyticsDashboardDto> {
    return this.http.get<CouponAnalyticsDashboardDto>(
      `${this.apiUrl}/seller/${sellerId}/dashboard`
    );
  }

  // ==================== NEW: Admin Automation Endpoints ====================

  /**
   * Execute all coupon automations (admin only)
   */
  executeAutomations(): Observable<any> {
    return this.http.post(`${this.apiUrl}/automation/execute`, {});
  }

  /**
   * Get automation logs for the last N days (admin only)
   */
  getAutomationLogs(days: number = 7): Observable<CouponAutomationLogDto[]> {
    const params = { days: days.toString() };
    return this.http.get<CouponAutomationLogDto[]>(
      `${this.apiUrl}/automation/logs`,
      { params }
    );
  }
}
