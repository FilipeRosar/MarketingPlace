// order.service.ts — VERSÃO FINAL (COM TOKEN)
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';
import { Order } from '../../models/order/order.model';
import { AuthService } from '../auth/auth.service';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private apiUrl = `${environment.apiUrl}/orders`;

  private getAuthHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': token ? `Bearer ${token}` : '',
      'Content-Type': 'application/json'
    });
  }

  getMyOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.apiUrl}/my-orders`, {
      headers: this.getAuthHeaders()
    });
  }

  getOrderById(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/${id}`, {
      headers: this.getAuthHeaders()
    });
  }

  updateTracking(orderId: string, trackingCode: string, carrier: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${orderId}/tracking`,
      { trackingCode, carrier },
      { headers: this.getAuthHeaders() }
    );
  }

  getTrackingUrl(trackingCode: string, carrier?: string): string {
    const code = trackingCode.trim().toUpperCase();

    if (!carrier || carrier.toLowerCase().includes('correios')) {
      return `https://rastreamento.correios.com.br/app/index.php?objeto=${code}`;
    }
    if (carrier.toLowerCase().includes('jadlog')) {
      return `https://www.jadlog.com.br/tracking/${code}`;
    }
    if (carrier.toLowerCase().includes('loggi')) {
      return `https://www.loggi.com/rastreio/${code}`;
    }
    return `https://www.melhorrastreio.com.br/rastreio/${code}`;
  }

  openTracking(code: string, carrier?: string): void {
    const url = this.getTrackingUrl(code, carrier);
    window.open(url, '_blank', 'width=800,height=600');
  }
}
