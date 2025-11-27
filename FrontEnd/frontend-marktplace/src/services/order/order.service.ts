import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { Order } from '../../models/order/order.model';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/orders`;

  private checkoutUrl = `${environment.apiUrl}/checkout/create-session`;

  constructor() { }

  createCheckoutSession(items: any[]): Observable<{ sessionId: string, url: string }> {
    return this.http.post<{ sessionId: string, url: string }>(this.checkoutUrl, {});
  }

  getMyOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(this.apiUrl);
  }

  getOrderById(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/${id}`);
  }
}
