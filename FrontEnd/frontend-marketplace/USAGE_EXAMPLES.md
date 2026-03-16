/**
 * SUBSCRIPTION ANALYTICS - USAGE EXAMPLES
 * 
 * This file contains practical examples of how to use the 
 * SubscriptionAnalyticsService in your Angular application.
 */

// ============================================================================
// Example 1: Using in Component Constructor (Recommended)
// ============================================================================

import { Component, OnInit, OnDestroy } from '@angular/core';
import { SubscriptionAnalyticsService, SubscriptionAnalyticsDashboard } from '../services/analytics/subscription-analytics.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-seller-dashboard',
  template: `
    <div *ngIf="dashboard">
      <h1>{{ dashboard.currentPlan }} Plan</h1>
      <p>Monthly Revenue: {{ dashboard.metrics.monthlyRecurringRevenue }}</p>
    </div>
  `
})
export class SellerDashboardComponent implements OnInit, OnDestroy {
  dashboard: SubscriptionAnalyticsDashboard | null = null;
  private destroy$ = new Subject<void>();

  constructor(private subscriptionAnalytics: SubscriptionAnalyticsService) {}

  ngOnInit() {
    this.subscriptionAnalytics.getSubscriptionDashboard()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.dashboard = data;
        },
        error: (err) => {
          console.error('Failed to load dashboard:', err);
        }
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}

// ============================================================================
// Example 2: Getting Specific Metrics Separately
// ============================================================================

export class MetricsDisplayComponent implements OnInit {
  roiPercent: number = 0;
  mrrValue: number = 0;

  constructor(private subscriptionAnalytics: SubscriptionAnalyticsService) {}

  ngOnInit() {
    // Get ROI Metrics
    this.subscriptionAnalytics.getROIAnalysis().subscribe({
      next: (roi) => {
        this.roiPercent = roi.roi;
        console.log(`ROI: ${roi.roi}%`);
        console.log(`Payback period: ${roi.paybackPeriod} months`);
      }
    });

    // Get Churn Risk
    this.subscriptionAnalytics.getChurnRiskAssessment().subscribe({
      next: (risk) => {
        if (risk.riskLevel === 'High') {
          console.warn('High churn risk detected!');
          console.warn('Factors:', risk.riskFactors);
        }
      }
    });

    // Get Upsell Opportunities
    this.subscriptionAnalytics.getUpsellOpportunities().subscribe({
      next: (opportunities) => {
        if (opportunities.length > 0) {
          const topOpp = opportunities[0];
          console.log(`Top opportunity: ${topOpp.fromPlan} → ${topOpp.toPlan}`);
          console.log(`Additional revenue: $${topOpp.expectedAdditionalRevenue}/mo`);
        }
      }
    });
  }
}

// ============================================================================
// Example 3: Displaying Trend Data for Charts
// ============================================================================

import { ChartData } from 'chart.js';

export class AnalyticsChartComponent implements OnInit {
  chartData: ChartData<'line'> | null = null;

  constructor(private subscriptionAnalytics: SubscriptionAnalyticsService) {}

  ngOnInit() {
    // Get 90 days of trend data for charting
    this.subscriptionAnalytics.getTrendData(90).subscribe({
      next: (trends) => {
        const labels = trends.map(t => t.date);
        const mrrData = trends.map(t => t.monthlyRecurringRevenue);
        
        this.chartData = {
          labels: labels,
          datasets: [
            {
              label: 'Monthly Recurring Revenue',
              data: mrrData,
              borderColor: '#3b82f6',
              backgroundColor: 'rgba(59, 130, 246, 0.1)',
              tension: 0.4
            }
          ]
        };
      }
    });
  }
}

// ============================================================================
// Example 4: Exporting Reports
// ============================================================================

export class ReportExportComponent {
  constructor(private subscriptionAnalytics: SubscriptionAnalyticsService) {}

  downloadPDFReport() {
    this.subscriptionAnalytics.exportAnalytics('pdf').subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `subscription-analytics-${new Date().toISOString().split('T')[0]}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Failed to export PDF:', err);
        alert('Failed to download report');
      }
    });
  }

  downloadCSVReport() {
    this.subscriptionAnalytics.exportAnalytics('csv').subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `subscription-analytics-${new Date().toISOString().split('T')[0]}.csv`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => console.error('Failed to export CSV:', err)
    });
  }
}

// ============================================================================
// Example 5: Conditional Rendering Based on Plan
// ============================================================================

export class PlanFeatureComponent implements OnInit {
  canViewAdvancedAnalytics = false;
  canAccessForecast = false;
  canExportReports = false;

  constructor(private subscriptionAnalytics: SubscriptionAnalyticsService) {}

  ngOnInit() {
    this.subscriptionAnalytics.getSubscriptionDashboard().subscribe({
      next: (dashboard) => {
        this.canViewAdvancedAnalytics = ['Pro', 'Premium'].includes(dashboard.currentPlan);
        this.canAccessForecast = dashboard.currentPlan === 'Premium';
        this.canExportReports = ['Pro', 'Premium'].includes(dashboard.currentPlan);
      }
    });
  }
}

// HTML Example:
/*
<div *ngIf="canViewAdvancedAnalytics" class="advanced-metrics">
  <h2>Advanced Analytics</h2>
  <!-- Advanced content -->
</div>

<div *ngIf="!canViewAdvancedAnalytics" class="upgrade-prompt">
  <p>Upgrade to Pro or Premium to access advanced analytics</p>
  <button (click)="navigateToUpgrade()">Upgrade Now</button>
</div>
*/

// ============================================================================
// Example 6: Building Custom Dashboards with Partial Data
// ============================================================================

export class CustomDashboardComponent implements OnInit {
  kpis = {
    mrr: 0,
    roi: 0,
    ltv: 0,
    payback: 0,
    churnRisk: 0,
    topOpportunity: null as any
  };

  constructor(private subscriptionAnalytics: SubscriptionAnalyticsService) {}

  ngOnInit() {
    // Load dashboard (has all metrics)
    this.subscriptionAnalytics.getSubscriptionDashboard().subscribe({
      next: (dashboard) => {
        this.kpis.mrr = dashboard.metrics.monthlyRecurringRevenue;
        this.kpis.ltv = dashboard.metrics.lifetimeValue;
      }
    });

    // Load ROI separately if you need detailed breakdown
    this.subscriptionAnalytics.getROIAnalysis().subscribe({
      next: (roi) => {
        this.kpis.roi = roi.roi;
        this.kpis.payback = roi.paybackPeriod;
      }
    });

    // Load opportunities and pick the best one
    this.subscriptionAnalytics.getUpsellOpportunities().subscribe({
      next: (opps) => {
        if (opps.length > 0) {
          // Sort by score and pick highest
          this.kpis.topOpportunity = opps.sort(
            (a, b) => b.upSellScore - a.upSellScore
          )[0];
        }
      }
    });
  }
}

// ============================================================================
// Example 7: Comparing Plans with Service
// ============================================================================

export class PlanComparisonComponent implements OnInit {
  currentPlanPrice = 0;
  nextPlanPrice = 0;
  upgradeCost = 0;
  expectedGain = 0;

  constructor(private subscriptionAnalytics: SubscriptionAnalyticsService) {}

  ngOnInit() {
    this.subscriptionAnalytics.getPlanComparison().subscribe({
      next: (comparison) => {
        this.currentPlanPrice = comparison.currentPlan.monthlyPrice;
        if (comparison.nextPlanUp) {
          this.nextPlanPrice = comparison.nextPlanUp.monthlyPrice;
          this.upgradeCost = comparison.additionalCostPerMonth;
        }
      }
    });

    // Get expected additional revenue from upgrade
    this.subscriptionAnalytics.getUpsellOpportunities().subscribe({
      next: (opps) => {
        if (opps.length > 0) {
          this.expectedGain = opps[0].expectedAdditionalRevenue;
        }
      }
    });
  }

  showROIOfUpgrade() {
    const monthsToPayback = this.upgradeCost / this.expectedGain;
    console.log(`Upgrade will pay for itself in ${monthsToPayback.toFixed(1)} months`);
  }
}

// ============================================================================
// Example 8: Real-time Monitoring Service
// ============================================================================

import { interval } from 'rxjs';
import { switchMap } from 'rxjs/operators';

export class RealTimeMonitoringComponent implements OnInit, OnDestroy {
  dashboard: SubscriptionAnalyticsDashboard | null = null;
  private destroy$ = new Subject<void>();
  private refreshInterval = 5 * 60 * 1000; // 5 minutes

  constructor(private subscriptionAnalytics: SubscriptionAnalyticsService) {}

  ngOnInit() {
    // Initial load
    this.loadDashboard();

    // Auto-refresh every 5 minutes
    interval(this.refreshInterval)
      .pipe(
        switchMap(() => this.subscriptionAnalytics.getSubscriptionDashboard()),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (dashboard) => {
          this.dashboard = dashboard;
          console.log('Dashboard refreshed at', new Date());
        }
      });
  }

  private loadDashboard() {
    this.subscriptionAnalytics.getSubscriptionDashboard()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => this.dashboard = data
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}

// ============================================================================
// Example 9: Error Handling & User Feedback
// ============================================================================

export class ErrorHandlingComponent implements OnInit {
  loading = false;
  error: string | null = null;
  success = false;

  constructor(
    private subscriptionAnalytics: SubscriptionAnalyticsService,
    private notificationService: NotificationService
  ) {}

  ngOnInit() {
    this.loadAnalytics();
  }

  loadAnalytics() {
    this.loading = true;
    this.error = null;

    this.subscriptionAnalytics.getSubscriptionDashboard().subscribe({
      next: (dashboard) => {
        this.loading = false;
        this.success = true;
        this.notificationService.success('Analytics loaded successfully');
      },
      error: (error) => {
        this.loading = false;
        this.error = error.error?.message || 'Failed to load analytics';
        this.notificationService.error(this.error);
      }
    });
  }

  retry() {
    this.loadAnalytics();
  }
}

// ============================================================================
// Example 10: Integration with NgRx Store (if using state management)
// ============================================================================

import { Store } from '@ngrx/store';
import { selectSubscriptionDashboard } from './store/subscription.selectors';

export class StoreIntegratedComponent implements OnInit {
  dashboard$ = this.store.select(selectSubscriptionDashboard);

  constructor(private store: Store) {}

  ngOnInit() {
    // Dispatch action to load analytics
    // this.store.dispatch(loadSubscriptionAnalytics());
  }
}

// ============================================================================
// SUMMARY
// 
// Key Points:
// 1. Always unsubscribe using takeUntil(this.destroy$) in ngOnDestroy
// 2. Use error callbacks for user-friendly error messages
// 3. Implement loading states for better UX
// 4. Consider caching for repeated API calls
// 5. Plan feature access based on subscription tier
// ============================================================================
