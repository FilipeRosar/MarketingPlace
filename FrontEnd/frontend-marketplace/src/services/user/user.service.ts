import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  uploadProfilePhoto(file: File): Observable<{ imageUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<{ imageUrl: string }>(`${this.apiUrl}/upload-photo`, formData);
  }

  updateProfile(data: any): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/profile`, data);
  }

  deleteAccount(password: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/account`, {
      body: { password }
    });
  }
}
