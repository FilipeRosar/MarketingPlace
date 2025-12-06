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

  constructor() { }

  getMyOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.apiUrl}/my-orders`);
  }
  updateTracking(orderId: string, trackingCode: string, carrier: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${orderId}/tracking`, { trackingCode, carrier });
  }
  getTrackingUrl(code: string, carrier: string): string {
    if (carrier.toLowerCase().includes('correios')) {
        return `https://rastreamento.correios.com.br/app/index.php?objeto=${code}`;
    }
    return `https://www.melhorrastreio.com.br/rastreio/${code}`;
  }

  getOrderById(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/${id}`);
  }
}
