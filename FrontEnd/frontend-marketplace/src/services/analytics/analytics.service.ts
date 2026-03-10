import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PlatformAnalyticsDto {
  totalGMV: number;
  totalOrders: number;
  totalUsers: number;
  totalSellers: number;
  totalProducts: number;
  platformRevenue: number;
  averageOrderValue: number;
  conversionRate: number;
  newUsersThisMonth: number;
  newOrdersThisMonth: number;
  growthRate: number;
}

export interface TopProductDto {
  productId: string;
  productName: string;
  sellerName: string;
  totalSales: number;
  totalQuantitySold: number;
  averageRating: number;
  totalReviews: number;
}

export interface UserAnalyticsDto {
  totalUsers: number;
  buyers: number;
  sellers: number;
  admins: number;
  newUsersThisMonth: number;
  activeUsersThisMonth: number;
  averageUserLifetimeValue: number;
}

export interface SalesPeriodDto {
  period: string;
  totalSales: number;
  totalOrders: number;
  averageOrderValue: number;
  newCustomers: number;
}

export interface CategoryDistributionDto {
  categoryId: string;
  categoryName: string;
  productCount: number;
  totalSales: number;
  percentage: number;
}

export interface PlatformHealthDto {
  pendingSellers: number;
  pendingOrders: number;
  lowStockProducts: number;
  inactiveListings: number;
  platformHealthScore: number;
}

export interface CommissionReportItemDto {
  sellerId: string;
  sellerName: string;
  totalSales: number;
  commissionEarned: number;
  rate: number;
}

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private apiUrl = `${environment.apiUrl}/analytics`;

  constructor(private http: HttpClient) { }

  // Platform Analytics
  getPlatformAnalytics(): Observable<PlatformAnalyticsDto> {
    return this.http.get<PlatformAnalyticsDto>(`${this.apiUrl}/platform`);
  }

  getTopProducts(limit: number = 10): Observable<TopProductDto[]> {
    return this.http.get<TopProductDto[]>(`${this.apiUrl}/top-products?limit=${limit}`);
  }

  getUserAnalytics(): Observable<UserAnalyticsDto> {
    return this.http.get<UserAnalyticsDto>(`${this.apiUrl}/users`);
  }

  getSalesByPeriod(): Observable<SalesPeriodDto[]> {
    return this.http.get<SalesPeriodDto[]>(`${this.apiUrl}/sales-period`);
  }

  getCategoryDistribution(): Observable<CategoryDistributionDto[]> {
    return this.http.get<CategoryDistributionDto[]>(`${this.apiUrl}/category-distribution`);
  }

  getPlatformHealth(): Observable<PlatformHealthDto> {
    return this.http.get<PlatformHealthDto>(`${this.apiUrl}/health`);
  }

  getSellerPerformance(): Observable<CommissionReportItemDto[]> {
    return this.http.get<CommissionReportItemDto[]>(`${this.apiUrl}/sellers`);
  }

  getConversionFunnel(): Observable<Record<string, number>> {
    return this.http.get<Record<string, number>>(`${this.apiUrl}/conversion-funnel`);
  }
}
