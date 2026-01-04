import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DashboardStats {
  totalGMV: number;
  totalOrders: number;
  newUsersLastMonth: number;
  platformRevenue: number;
  pendingApprovals: number;
}

export interface PendingSeller {
  id: string;
  name: string;
  email: string;
  bio: string;
  createdAt: string;
}

export interface CommissionReportItem {
  sellerId: string;
  sellerName: string;
  totalSales: number;
  commissionEarned: number;
  rate: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin`;

  getDashboardStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.apiUrl}/sales-by-month`);
  }

  getPendingSellers(): Observable<PendingSeller[]> {
    return this.http.get<PendingSeller[]>(`${this.apiUrl}/sellers/pending`);
  }

  approveSeller(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/approve-seller/${id}`, {});
  }

  rejectSeller(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reject-seller/${id}`, {});
  }

  getCustomers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/customers`);
  }

  getCommissionReport(): Observable<CommissionReportItem[]> {
    return this.http.get<CommissionReportItem[]>(`${this.apiUrl}/commission-report`);
  }

  updateCommissionRate(rate: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/commission-rate`, { rate });
  }

  setSellerCommission(sellerId: string, rate: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/sellers/${sellerId}/commission`, rate);
  }

  updateServiceFee(fee: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/service-fee`, { fee });
  }

  // --- O MÉTODO QUE FALTAVA ---
  getServiceFee(): Observable<{ fee: number }> {
    return this.http.get<{ fee: number }>(`${this.apiUrl}/settings/service-fee`);
  }

  getSalesByMonth(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/sales-by-month`);
  }
}
