import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

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

  searchSellers(query: string): Observable<any[]> {
    const params = new HttpParams().set('search', query);
    return this.http.get<any[]>(this.apiUrl, { params });
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
