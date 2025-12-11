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
  bio: string | null;
  createdAt: string;
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
