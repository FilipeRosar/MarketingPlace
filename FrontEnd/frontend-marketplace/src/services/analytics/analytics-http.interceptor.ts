import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
  HttpResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { EventTrackingService } from './event-tracking.service';

/**
 * Interceptor que rastreia requisições HTTP e possíveis erros
 */
@Injectable()
export class AnalyticsHttpInterceptor implements HttpInterceptor {
  
  private requestStartTime: number = 0;

  constructor(private eventTrackingService: EventTrackingService) { }

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    
    // Iniciar cronômetro
    this.requestStartTime = performance.now();

    return next.handle(request).pipe(
      tap((event: HttpEvent<any>) => {
        // Log de sucesso
        if (event instanceof HttpResponse) {
          const duration = performance.now() - this.requestStartTime;
          
          // Rastrear apenas endpoints específicos
          this.trackSuccessfulRequest(request, event, duration);
        }
      }),
      catchError((error: HttpErrorResponse) => {
        const duration = performance.now() - this.requestStartTime;
        this.trackFailedRequest(request, error, duration);
        return throwError(() => error);
      })
    );
  }

  /**
   * Rastreia requisições bem-sucedidas
   */
  private trackSuccessfulRequest(
    request: HttpRequest<any>,
    response: HttpResponse<any>,
    duration: number
  ): void {
    
    // Rastrear endpoints críticos de pagamento/checkout
    if (this.isCheckoutEndpoint(request.url)) {
      this.eventTrackingService.trackCustomEvent('api_call_success', {
        endpoint: this.getEndpointName(request.url),
        method: request.method,
        statusCode: response.status,
        duration: Math.round(duration)
      });
    }

    // Rastrear erros de API (status 4xx/5xx)
    if (response.status >= 400) {
      this.eventTrackingService.trackError(
        `API Error: ${response.status}`,
        'HttpError',
        this.getEndpointName(request.url)
      );
    }
  }

  /**
   * Rastreia requisições falhadas
   */
  private trackFailedRequest(
    request: HttpRequest<any>,
    error: HttpErrorResponse,
    duration: number
  ): void {
    
    this.eventTrackingService.trackError(
      `HTTP ${error.status}: ${error.statusText}`,
      'HttpInterceptorError',
      this.getEndpointName(request.url),
    );

    if (this.isCheckoutEndpoint(request.url)) {
      this.eventTrackingService.trackCustomEvent('api_call_failure', {
        endpoint: this.getEndpointName(request.url),
        method: request.method,
        statusCode: error.status,
        statusText: error.statusText,
        duration: Math.round(duration)
      });
    }
  }

  /**
   * Verifica se é um endpoint de checkout
   */
  private isCheckoutEndpoint(url: string): boolean {
    const checkoutPaths = [
      '/checkout',
      '/payment',
      '/order',
      '/shipping',
      '/coupon',
      '/cart'
    ];
    return checkoutPaths.some(path => url.includes(path));
  }

  /**
   * Extrai nome do endpoint da URL
   */
  private getEndpointName(url: string): string {
    try {
      const urlObj = new URL(url);
      const pathname = urlObj.pathname;
      const parts = pathname.split('/').filter(p => p);
      return parts[parts.length - 1] || 'unknown';
    } catch {
      return 'unknown';
    }
  }
}
