import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { MomentResponseDto } from '../../models/moment/moment.model';

@Injectable({
  providedIn: 'root'
})
export class SellerService {
  private apiUrl = `${environment.apiUrl}/sellers`;
  private userApiUrl = `${environment.apiUrl}/users`;
  private http = inject(HttpClient);

  getSellerById(id: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }
  getSellerByUserId(userId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/by-user/${userId}`);
  }
  searchSellers(query: string, limit: number = 6): Observable<any[]> {
    let params = new HttpParams().set('search', query);
    if (limit) params = params.set('limit', limit.toString());
    return this.http.get<any[]>(this.apiUrl, { params });
  }
  uploadMomentVideo(sellerId: string, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('video', file);
    return this.http.post(`${this.apiUrl}/${sellerId}/moments/upload-video`, formData);
  }

  uploadMomentThumb(sellerId: string, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('thumb', file);
    return this.http.post(`${this.apiUrl}/${sellerId}/moments/upload-thumb`, formData);
  }
  getMoments(sellerId: string): Observable<MomentResponseDto[]> {
    return this.http.get<MomentResponseDto[]>(`${this.apiUrl}/${sellerId}/moments`);
  }
  getDashboardData() {
  return this.http.get<any>(`${this.apiUrl}/dashboard`);
  }
  createMoment(sellerId: string, dto: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/${sellerId}/moments`, dto);
  }
  updateProfile(data: any): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/profile`, data);
  }

  uploadBanner(file: File): Observable<{ imageUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ imageUrl: string }>(`${this.userApiUrl}/upload-banner`, formData);
  }
}
