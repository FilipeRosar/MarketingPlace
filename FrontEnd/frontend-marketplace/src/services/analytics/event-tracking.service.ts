import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

/**
 * Interface para eventos de analytics
 */
export interface AnalyticsEvent {
  eventName: string;
  eventCategory?: string;
  eventLabel?: string;
  eventValue?: number | string;
  customData?: Record<string, any>;
  timestamp?: Date;
}

/**
 * Interface para eventos de checkout
 */
export interface CheckoutEvent {
  userId?: string;
  step: 'add_to_cart' | 'view_cart' | 'begin_checkout' | 'add_shipping_info' | 'add_payment_info' | 'purchase' | 'refund';
  currency?: string;
  value?: number;
  items?: any[];
  coupon?: string;
  shippingCost?: number;
}

/**
 * Interface para eventos de produtos
 */
export interface ProductEvent {
  productId: string;
  productName: string;
  productCategory?: string;
  price?: number;
  quantity?: number;
  eventType: 'view_item' | 'add_to_cart' | 'remove_from_cart' | 'purchase' | 'add_to_wishlist' | 'remove_from_wishlist';
}

@Injectable({
  providedIn: 'root'
})
export class EventTrackingService {
  
  private analyticsEnabled: boolean = !environment.production ? false : true;
  private eventQueue: AnalyticsEvent[] = [];
  private batchSize = 10;
  private flushInterval = 30000; // 30 segundos
  private flushTimer: any;

  constructor(private http: HttpClient) {
    this.initializeBatchTimer();
  }

  /**
   * Rastreia um evento genérico
   */
  trackEvent(event: AnalyticsEvent): void {
    if (!this.analyticsEnabled) {
      console.log('[ANALYTICS] Event (development mode):', event);
      return;
    }

    event.timestamp = new Date();
    this.eventQueue.push(event);

    // Enviar imediatamente se atingir tamanho do lote
    if (this.eventQueue.length >= this.batchSize) {
      this.flushEvents();
    }
  }

  /**
   * Rastreia evento de visualização de produto
   */
  trackProductView(product: any): void {
    this.trackEvent({
      eventName: 'view_item',
      eventCategory: 'Products',
      eventLabel: product.name,
      customData: {
        productId: product.id,
        productName: product.name,
        price: product.price,
        category: product.category,
        sellerId: product.sellerId
      }
    });
  }

  /**
   * Rastreia adição ao carrinho
   */
  trackAddToCart(product: any, quantity: number = 1): void {
    this.trackEvent({
      eventName: 'add_to_cart',
      eventCategory: 'Cart',
      eventLabel: product.name,
      eventValue: quantity,
      customData: {
        productId: product.id,
        productName: product.name,
        price: product.price,
        quantity: quantity,
        total: product.price * quantity
      }
    });
  }

  /**
   * Rastreia remoção do carrinho
   */
  trackRemoveFromCart(product: any, quantity: number = 1): void {
    this.trackEvent({
      eventName: 'remove_from_cart',
      eventCategory: 'Cart',
      eventLabel: product.name,
      eventValue: quantity,
      customData: {
        productId: product.id,
        productName: product.name,
        price: product.price,
        quantity: quantity
      }
    });
  }

  /**
   * Rastreia visualização do carrinho
   */
  trackViewCart(cartTotal: number, itemCount: number): void {
    this.trackEvent({
      eventName: 'view_cart',
      eventCategory: 'Cart',
      eventValue: cartTotal,
      customData: {
        cartTotal: cartTotal,
        itemCount: itemCount
      }
    });
  }

  /**
   * Rastreia início do checkout
   */
  trackBeginCheckout(cartTotal: number, itemCount: number, coupon?: string): void {
    this.trackEvent({
      eventName: 'begin_checkout',
      eventCategory: 'Checkout',
      eventValue: cartTotal,
      customData: {
        cartTotal: cartTotal,
        itemCount: itemCount,
        coupon: coupon
      }
    });
  }

  /**
   * Rastreia seleção de método de envio
   */
  trackShippingInfoAdded(shippingCost: number, shippingMethod: string, sellers: number = 1): void {
    this.trackEvent({
      eventName: 'add_shipping_info',
      eventCategory: 'Checkout',
      eventValue: shippingCost,
      customData: {
        shippingCost: shippingCost,
        shippingMethod: shippingMethod,
        numberOfSellers: sellers
      }
    });
  }

  /**
   * Rastreia adição de informações de pagamento
   */
  trackPaymentInfoAdded(paymentMethod: string, installments?: number): void {
    this.trackEvent({
      eventName: 'add_payment_info',
      eventCategory: 'Checkout',
      customData: {
        paymentMethod: paymentMethod,
        installments: installments || 1
      }
    });
  }

  /**
   * Rastreia aplicação de cupom
   */
  trackCouponApplied(couponCode: string, discountAmount: number): void {
    this.trackEvent({
      eventName: 'apply_coupon',
      eventCategory: 'Checkout',
      eventLabel: couponCode,
      eventValue: discountAmount,
      customData: {
        couponCode: couponCode,
        discountAmount: discountAmount
      }
    });
  }

  /**
   * Rastreia compra
   */
  trackPurchase(orderId: string, total: number, items: any[], currency: string = 'BRL'): void {
    this.trackEvent({
      eventName: 'purchase',
      eventCategory: 'Checkout',
      eventValue: total,
      customData: {
        orderId: orderId,
        total: total,
        currency: currency,
        items: items,
        itemCount: items.length,
        timestamp: new Date().toISOString()
      }
    });
  }

  /**
   * Rastreia reembolso/cancelamento
   */
  trackRefund(orderId: string, refundAmount: number, reason?: string): void {
    this.trackEvent({
      eventName: 'refund',
      eventCategory: 'Order',
      eventValue: refundAmount,
      customData: {
        orderId: orderId,
        refundAmount: refundAmount,
        reason: reason
      }
    });
  }

  /**
   * Rastreia busca
   */
  trackSearch(searchTerm: string, resultCount: number): void {
    this.trackEvent({
      eventName: 'search',
      eventCategory: 'Search',
      eventLabel: searchTerm,
      eventValue: resultCount,
      customData: {
        searchTerm: searchTerm,
        resultCount: resultCount
      }
    });
  }

  /**
   * Rastreia login
   */
  trackLogin(userId: string, method?: string): void {
    this.trackEvent({
      eventName: 'login',
      eventCategory: 'User',
      eventLabel: method || 'email',
      customData: {
        userId: userId,
        method: method || 'email'
      }
    });
  }

  /**
   * Rastreia registro
   */
  trackSignUp(userId: string, method?: string): void {
    this.trackEvent({
      eventName: 'sign_up',
      eventCategory: 'User',
      eventLabel: method || 'email',
      customData: {
        userId: userId,
        method: method || 'email'
      }
    });
  }

  /**
   * Rastreia visualização de página
   */
  trackPageView(pageName: string, pageUrl?: string): void {
    this.trackEvent({
      eventName: 'page_view',
      eventCategory: 'Navigation',
      eventLabel: pageName,
      customData: {
        pageName: pageName,
        pageUrl: pageUrl || window.location.href
      }
    });
  }

  /**
   * Rastreia click em botão
   */
  trackButtonClick(buttonLabel: string, context?: string): void {
    this.trackEvent({
      eventName: 'button_click',
      eventCategory: 'Interaction',
      eventLabel: buttonLabel,
      customData: {
        buttonLabel: buttonLabel,
        context: context
      }
    });
  }

  /**
   * Rastreia erro da aplicação
   */
  trackError(errorMessage: string, errorType?: string, errorContext?: string): void {
    this.trackEvent({
      eventName: 'error',
      eventCategory: 'System',
      eventLabel: errorMessage,
      customData: {
        errorMessage: errorMessage,
        errorType: errorType,
        errorContext: errorContext,
        url: window.location.href
      }
    });
  }

  /**
   * Rastreia evento customizado
   */
  trackCustomEvent(eventName: string, data?: Record<string, any>): void {
    this.trackEvent({
      eventName: eventName,
      eventCategory: 'Custom',
      customData: data
    });
  }

  /**
   * Envia eventos em lote
   */
  private flushEvents(): void {
    if (this.eventQueue.length === 0) {
      return;
    }

    const eventsToSend = [...this.eventQueue];
    this.eventQueue = [];

    if (this.analyticsEnabled) {
      this.http.post(`${environment.apiUrl}/api/analytics/events`, {
        events: eventsToSend
      }).subscribe({
        next: () => {
          console.log('[ANALYTICS] Events sent successfully:', eventsToSend.length);
        },
        error: (err) => {
          console.error('[ANALYTICS] Failed to send events:', err);
          // Requeue eventos em caso de falha (limite razoável)
          if (this.eventQueue.length < 100) {
            this.eventQueue.push(...eventsToSend);
          }
        }
      });
    }
  }

  /**
   * Inicia timer para envio periódico de eventos
   */
  private initializeBatchTimer(): void {
    this.flushTimer = setInterval(() => {
      this.flushEvents();
    }, this.flushInterval);
  }

  /**
   * Limpa recursos ao destruir o serviço
   */
  ngOnDestroy(): void {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
    }
    this.flushEvents();
  }
}
