import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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
    return this.http.get<DashboardStats>(`${this.apiUrl}/dashboard-stats`);
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

  // --- O MÃ‰TODO QUE FALTAVA ---
  getServiceFee(): Observable<{ fee: number }> {
    return this.http.get<{ fee: number }>(`${this.apiUrl}/settings/service-fee`);
  }
  getCommissionRate(): Observable<{ rate: number }> {
    return this.http.get<{ rate: number }>(`${this.apiUrl}/settings/commission-rate`);
  }
  getSalesByMonth(start?: string, end?: string): Observable<any[]> {
    let params = new HttpParams();
    if (start) params = params.set('start', start);
    if (end) params = params.set('end', end);
    return this.http.get<any[]>(`${this.apiUrl}/sales-by-month`, { params });
  }
}





