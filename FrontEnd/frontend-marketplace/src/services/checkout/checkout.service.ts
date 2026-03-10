import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CheckoutService {
  private http = inject(HttpClient);

  // URL para criar a sessão de pagamento
  private checkoutUrl = `${environment.apiUrl}/checkout/create-session`;

  constructor() { }

  createCheckoutSession(items: any[], shippingFee: number = 0, shippingName: string = 'Frete'): Observable<{ sessionId: string; url: string }> {
    const itemsList = items.map(item => ({
      productId: item.product.id,
      quantity: item.quantity
    }));

    const payload = {
      items: itemsList,
      shippingFee,
      shippingName,
      successUrl: window.location.origin + '/#/orders',
      cancelUrl:  window.location.origin + '/#/cart'
    };

    return this.http.post<{ sessionId: string; url: string }>(this.checkoutUrl, payload);
  }

  createCheckoutSessionWithShipping(items: any[], shippingData: Array<{ sellerId: string; shippingOption: any }>, couponCode?: string): Observable<{ sessionId: string; url: string }> {
    const itemsList = items.map(item => ({
      productId: item.product.id,
      quantity: item.quantity,
      sellerId: item.product.sellerId
    }));

    const payload: any = {
      items: itemsList,
      shippingData: shippingData.map(sd => ({
        sellerId: sd.sellerId,
        shippingName: sd.shippingOption.name,
        shippingFee: sd.shippingOption.price
      })),
      successUrl: window.location.origin + '/#/orders',
      cancelUrl:  window.location.origin + '/#/cart'
    };

    if (couponCode) {
      payload.couponCode = couponCode;
    }

    return this.http.post<{ sessionId: string; url: string }>(this.checkoutUrl, payload);
  }
}

