import { Injectable } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { EventTrackingService } from './event-tracking.service';

/**
 * Serviço que rastreia automaticamente navegação de páginas
 */
@Injectable({
  providedIn: 'root'
})
export class PageTrackingService {
  
  private previousUrl: string = '';

  constructor(
    private router: Router,
    private eventTrackingService: EventTrackingService
  ) {
    this.initializePageTracking();
  }

  /**
   * Inicia rastreamento automático de mudanças de página
   */
  private initializePageTracking(): void {
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      if (event instanceof NavigationEnd) {
        const pageTitle = this.getTitleFromRoute();
        this.eventTrackingService.trackPageView(pageTitle || 'Unknown Page', event.urlAfterRedirects);
        this.previousUrl = event.urlAfterRedirects;
      }
    });
  }

  /**
   * Extrai o título da página a partir da rota
   */
  private getTitleFromRoute(): string {
    const urlSegments = this.router.url.split('/').filter(s => s);
    if (urlSegments.length === 0) return 'Home';
    
    const lastSegment = urlSegments[urlSegments.length - 1];
    
    // Mapa de rotas conhecidas para títulos amigáveis
    const routeMap: Record<string, string> = {
      'home': 'Home',
      'products': 'Products',
      'product': 'Product Detail',
      'cart': 'Shopping Cart',
      'checkout': 'Checkout',
      'order': 'Order',
      'orders': 'My Orders',
      'profile': 'User Profile',
      'settings': 'Settings',
      'login': 'Login',
      'register': 'Register',
      'seller': 'Seller Dashboard',
      'sellers': 'Sellers',
      'admin': 'Admin Dashboard',
      'favorites': 'Favorites',
      'search': 'Search Results',
      'category': 'Category'
    };

    return routeMap[lastSegment] || this.formatString(lastSegment);
  }

  /**
   * Formata string em forma legível
   */
  private formatString(str: string): string {
    return str
      .replace(/-/g, ' ')
      .split(' ')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ');
  }

  /**
   * Retorna a URL anterior
   */
  getPreviousUrl(): string {
    return this.previousUrl;
  }
}
