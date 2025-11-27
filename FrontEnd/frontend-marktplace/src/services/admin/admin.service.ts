import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private apiUrl = `${environment.apiUrl}/admin`;
  private http = inject(HttpClient);

  getPendingSellers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/sellers/pending`);
  }

  approveSeller(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/sellers/${id}/approve`, {});
  }

  rejectSeller(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/sellers/${id}/reject`);
  }

  getStats(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/stats`);
  }
}
