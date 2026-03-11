import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * ===========================
 * SUBSCRIPTION MODELS
 * ===========================
 */

export interface SubscriptionAnalyticsDashboard {
  sellerId: string;
  sellerName: string;
  currentPlan: 'Basic' | 'Pro' | 'Premium';
  subscriptionStatus: 'Active' | 'Expired' | 'Cancelled' | 'PendingRenewal';
  activeSubscription: SubscriptionDetails;
  metrics: SubscriptionMetrics;
  planComparison: PlanComparison;
  roiAnalysis: SubscriptionROI;
  churnRiskAssessment: ChurnRiskAssessment;
  upsellOpportunities: UpsellOpportunity[];
  generatedAt: string;
}

export interface SubscriptionDetails {
  planName: string;
  monthlyPrice: number;
  commissionRate: number;
  startDate: string;
  renewalDate: string;
  daysRemaining: number;
  autoRenew: boolean;
  features: SubscriptionFeature[];
}

export interface SubscriptionFeature {
  name: string;
  enabled: boolean;
  icon?: string;
}

export interface SubscriptionMetrics {
  monthlyRecurringRevenue: number; // MRR
  lifetimeValue: number;
  totalInvestmentToDate: number;
  averageMonthlyProfit: number;
  roi: number;
  planDuration: number; // in months
  upgradeCount: number;
  downgradeCount: number;
  planChangeHistory: PlanChange[];
}

export interface PlanChange {
  fromPlan: string;
  toPlan: string;
  changeDate: string;
  changeType: 'upgrade' | 'downgrade' | 'switch';
}

export interface PlanComparison {
  currentPlan: PlanTierComparison;
  nextPlanUp: PlanTierComparison | null;
  benefitsOfUpgrade: string[];
  additionalCostPerMonth: number;
}

export interface PlanTierComparison {
  planName: string;
  monthlyPrice: number;
  commissionRate: number;
  highlightLimit: number;
  hasAdvancedAnalytics: boolean;
  hasVerifiedBadge: boolean;
  hasPrioritySupport: boolean;
}

export interface SubscriptionROI {
  paybackPeriod: number; // in months
  breakEvenDate: string;
  profitVsInvestment: number; // profit value
  averageMonthlyIncrementalRevenue: number;
  projectedYearlyAdditionalRevenue: number;
  estimatedPaybackMonthly: number;
  performanceScore: number; // 1-100
  trend: 'improving' | 'stable' | 'declining';
}

export interface ChurnRiskAssessment {
  riskLevel: 'Low' | 'Medium' | 'High';
  riskScore: number; // 1-100
  riskFactors: ChurnRiskFactor[];
  daysAtRisk: number | null;
  recommendedAction: string;
  lastRiskEvaluation: string;
}

export interface ChurnRiskFactor {
  factor: string;
  severity: 'Low' | 'Medium' | 'High';
  description: string;
  suggestion: string;
}

export interface UpsellOpportunity {
  opportunityId: string;
  fromPlan: string;
  toPlan: string;
  upSellScore: number; // 0-100
  reasoning: string;
  expectedAdditionalRevenue: number;
  estimatedPayback: number;
  successProbability: number; // percentage
  timeSensitivity: 'Low' | 'Medium' | 'High';
}

export interface SubscriptionMetricsBreakdown {
  revenueByPlan: { [planName: string]: number };
  customersPerPlan: { [planName: string]: number };
  churnRateByPlan: { [planName: string]: number };
  upsellRateByPlan: { [planName: string]: number };
  averageCustomerLifetimeByPlan: { [planName: string]: number };
}

export interface SubscriptionTrendData {
  date: string;
  monthlyRecurringRevenue: number;
  activeSubscriptions: number;
  churnedCustomers: number;
  newSubscriptions: number;
}

/**
 * ===========================
 * SERVICE
 * ===========================
 */

@Injectable({
  providedIn: 'root'
})
export class SubscriptionAnalyticsService {
  private apiUrl = `${environment.apiUrl}/sellers/subscription-analytics`;

  constructor(private http: HttpClient) { }

  /**
   * Get complete subscription analytics dashboard
   */
  getSubscriptionDashboard(): Observable<SubscriptionAnalyticsDashboard> {
    return this.http.get<SubscriptionAnalyticsDashboard>(`${this.apiUrl}/dashboard`);
  }

  /**
   * Get ROI analysis for current subscription
   */
  getROIAnalysis(): Observable<SubscriptionROI> {
    return this.http.get<SubscriptionROI>(`${this.apiUrl}/roi`);
  }

  /**
   * Get churn risk assessment
   */
  getChurnRiskAssessment(): Observable<ChurnRiskAssessment> {
    return this.http.get<ChurnRiskAssessment>(`${this.apiUrl}/churn-risks`);
  }

  /**
   * Get upsell opportunities for current seller
   */
  getUpsellOpportunities(): Observable<UpsellOpportunity[]> {
    return this.http.get<UpsellOpportunity[]>(`${this.apiUrl}/upsell-opportunities`);
  }

  /**
   * Get detailed plan comparison
   */
  getPlanComparison(): Observable<PlanComparison> {
    return this.http.get<PlanComparison>(`${this.apiUrl}/plan-comparison`);
  }

  /**
   * Get subscription metrics by time period
   */
  getMetricsByPeriod(days: number = 30): Observable<SubscriptionMetrics> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<SubscriptionMetrics>(
      `${this.apiUrl}/metrics`,
      { params }
    );
  }

  /**
   * Get subscription trend data (for charts)
   */
  getTrendData(days: number = 90): Observable<SubscriptionTrendData[]> {
    const params = new HttpParams().set('days', days.toString());
    return this.http.get<SubscriptionTrendData[]>(
      `${this.apiUrl}/trends`,
      { params }
    );
  }

  /**
   * Get correlation between subscription tier and sales performance
   */
  getSalesCorrelation(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/sales-correlation`);
  }

  /**
   * Get subscription history (plan changes)
   */
  getSubscriptionHistory(): Observable<PlanChange[]> {
    return this.http.get<PlanChange[]>(`${this.apiUrl}/history`);
  }

  /**
   * Get current active subscription details
   */
  getActiveSubscription(): Observable<SubscriptionDetails> {
    return this.http.get<SubscriptionDetails>(`${this.apiUrl}/current`);
  }

  /**
   * Export subscription analytics as PDF/CSV
   */
  exportAnalytics(format: 'pdf' | 'csv'): Observable<Blob> {
    const params = new HttpParams().set('format', format);
    return this.http.get(
      `${this.apiUrl}/export`,
      { params, responseType: 'blob' }
    );
  }
}
