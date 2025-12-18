import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Customer } from '../../models/custumer/customer.model';

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
  bio: string | null;
  createdAt: string;
}
export interface SalesByMonth {
  month: string;
  total: number;
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
    return this.http.get<PendingSeller[]>(`${this.apiUrl}/pending-sellers`);
  }
  getCustomers(): Observable<Customer[]> {
    return this.http.get<Customer[]>(`${this.apiUrl}/customers`);
  }
  getSalesByMonth(): Observable<SalesByMonth[]> {
    return this.http.get<SalesByMonth[]>(`${this.apiUrl}/sales-by-month`);
  }
  approveSeller(sellerId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/approve-seller/${sellerId}`, {});
  }

  rejectSeller(sellerId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/reject-seller/${sellerId}`, {});
  }

  updateCommissionRate(rate: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/commission-rate`, { rate });
  }

  updateServiceFee(fee: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/service-fee`, { fee });
  }
}
