import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { environment } from '../../environments/environment';
import { tap } from 'rxjs/operators';

export enum SellerPlan {
  Basic = 0,
  Pro = 1,
  Premium = 2
}

export interface SellerSubscription {
  id: string;
  sellerId: string;
  plan: SellerPlan;
  isActive: boolean;
  startedAt: string;
  expiresAt: string | null;
  commissionRate: number;
  monthlyPrice: number;
  canHighlightProducts: boolean;
  highlightLimit: number;
  hasVerifiedBadge: boolean;
  hasAdvancedAnalytics: boolean;
  hasPrioritySupport: boolean;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UpgradePlanRequest {
  newPlan: SellerPlan;
  paymentMethodId?: string;
}

export interface SubscriptionResponse {
  success: boolean;
  message: string;
  subscription?: SellerSubscription;
}

export interface PlanInfo {
  plan: SellerPlan;
  name: string;
  monthlyPrice: number;
  commissionRate: number;
  features: string[];
}

@Injectable({
  providedIn: 'root'
})
export class SellerSubscriptionService {
  private apiUrl = `${environment.apiUrl}/sellers/subscription`;
  private currentSubscription$ = new BehaviorSubject<SellerSubscription | null>(null);
  private isLoading$ = new BehaviorSubject(false);

  private planInfos: PlanInfo[] = [
    {
      plan: SellerPlan.Basic,
      name: 'Plano Básico',
      monthlyPrice: 0,
      commissionRate: 12,
      features: [
        'Listar produtos',
        'Gerenciar pedidos',
        'Suporte por email',
        'Estatísticas básicas'
      ]
    },
    {
      plan: SellerPlan.Pro,
      name: 'Plano Pro',
      monthlyPrice: 29.99,
      commissionRate: 9,
      features: [
        'Tudo do plano Básico',
        '📊 Analytics avançado',
        'Destaque de produtos (até 8)',
        'Selo de vendedor verificado',
        'Análise de período',
        'Análise de clientes',
        'Suporte prioritário'
      ]
    },
    {
      plan: SellerPlan.Premium,
      name: 'Plano Premium',
      monthlyPrice: 59.90,
      commissionRate: 5,
      features: [
        'Tudo do plano Pro',
        '🤖 Insights com IA',
        'Previsão de vendas',
        'Segmentação de clientes',
        'Análise sazonal',
        'Destaque de produtos (até 15)',
        'Exportar relatórios (PDF/CSV)',
        'Suporte prioritário 24/7'
      ]
    }
  ];

  constructor(private http: HttpClient) { }

  /**
   * Get current seller subscription
   */
  getCurrentSubscription(): Observable<SellerSubscription> {
    this.isLoading$.next(true);
    return this.http.get<SellerSubscription>(`${this.apiUrl}/current`)
      .pipe(
        tap(subscription => {
          this.currentSubscription$.next(subscription);
          this.isLoading$.next(false);
        })
      );
  }

  /**
   * Get subscription as observable (cached)
   */
  getCurrentSubscription$(): Observable<SellerSubscription | null> {
    return this.currentSubscription$.asObservable();
  }

  /**
   * Refresh subscription data from server
   */
  refreshSubscription(): Observable<SellerSubscription> {
    return this.getCurrentSubscription();
  }

  /**
   * Upgrade to a new plan
   */
  upgradePlan(newPlan: SellerPlan): Observable<SubscriptionResponse> {
    const request: UpgradePlanRequest = { newPlan };
    return this.http.post<SubscriptionResponse>(`${this.apiUrl}/upgrade`, request)
      .pipe(
        tap(response => {
          if (response.subscription) {
            this.currentSubscription$.next(response.subscription);
          }
        })
      );
  }

  /**
   * Check if seller has specific plan
   */
  hasPlan(plan: SellerPlan): boolean {
    const current = this.currentSubscription$.value;
    return current ? current.plan >= plan : false;
  }

  /**
   * Check if seller has advanced analytics access
   */
  hasAdvancedAnalytics(): boolean {
    const current = this.currentSubscription$.value;
    return current ? current.hasAdvancedAnalytics : false;
  }

  /**
   * Get plan name
   */
  getPlanName(plan: SellerPlan): string {
    return this.planInfos.find(p => p.plan === plan)?.name || 'Desconhecido';
  }

  /**
   * Get plan info
   */
  getPlanInfo(plan: SellerPlan): PlanInfo | undefined {
    return this.planInfos.find(p => p.plan === plan);
  }

  /**
   * Get all plan infos for comparison
   */
  getAllPlans(): PlanInfo[] {
    return this.planInfos;
  }

  /**
   * Get loading status
   */
  isLoading(): Observable<boolean> {
    return this.isLoading$.asObservable();
  }

  /**
   * Get current plan value synchronously (use sparingly)
   */
  getCurrentPlanValue(): SellerPlan | null {
    return this.currentSubscription$.value?.plan ?? null;
  }

  /**
   * Check if plan is active
   */
  isPlanActive(): boolean {
    const current = this.currentSubscription$.value;
    if (!current) return false;
    
    if (!current.isActive) return false;
    
    if (current.expiresAt) {
      return new Date(current.expiresAt) > new Date();
    }
    
    return true;
  }

  /**
   * Get days until expiration
   */
  getDaysUntilExpiration(): number | null {
    const current = this.currentSubscription$.value;
    if (!current?.expiresAt) return null;

    const expiresAt = new Date(current.expiresAt);
    const now = new Date();
    const daysLeft = Math.ceil((expiresAt.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    
    return daysLeft > 0 ? daysLeft : 0;
  }

  /**
   * Format price with Brazilian Real
   */
  formatPrice(price: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(price);
  }

  /**
   * Get discount percentage compared to Basic plan
   */
  getCommissionDiscount(plan: SellerPlan): number {
    const basicRate = 12;
    const currentRate = this.planInfos.find(p => p.plan === plan)?.commissionRate || basicRate;
    return basicRate - currentRate;
  }
}
