import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RatingDto {
  id: string;
  customerId: string;
  customerName: string;
  stars: number;
  review: string;
  sellerReply?: string | null;
  sellerReplyAt?: string | null;
  createdAt: string;
}

export interface RatingsPage {
  data: RatingDto[];
  total: number;
  averageRating: number;
  page: number;
  pageSize: number;
  pages: number;
}

@Injectable({
  providedIn: 'root'
})
export class RatingsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/ratings`;

  getByProduct(productId: string, page: number = 1, pageSize: number = 10): Observable<RatingsPage> {
    return this.http.get<RatingsPage>(`${this.apiUrl}/product/${productId}?page=${page}&pageSize=${pageSize}`);
  }

  createRating(productId: string, stars: number, review: string): Observable<{ message: string; ratingId: string }> {
    return this.http.post<{ message: string; ratingId: string }>(`${this.apiUrl}`, {
      productId,
      stars,
      review
    });
  }

  replyToRating(ratingId: string, reply: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${ratingId}/reply`, {
      reply
    });
  }
}
