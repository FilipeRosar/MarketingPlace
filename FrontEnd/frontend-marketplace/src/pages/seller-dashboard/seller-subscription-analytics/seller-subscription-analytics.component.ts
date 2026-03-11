import { Component, OnInit, OnDestroy } from '@angular/core';
import { SubscriptionAnalyticsService, SubscriptionAnalyticsDashboard, ChurnRiskAssessment, UpsellOpportunity, SubscriptionROI } from '../../../services/analytics/subscription-analytics.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-seller-subscription-analytics',
  templateUrl: './seller-subscription-analytics.component.html',
  styleUrls: ['./seller-subscription-analytics.component.css']
})
export class SellerSubscriptionAnalyticsComponent implements OnInit, OnDestroy {
  dashboard: SubscriptionAnalyticsDashboard | null = null;
  churnRisk: ChurnRiskAssessment | null = null;
  upsellOpportunities: UpsellOpportunity[] = [];
  roiAnalysis: SubscriptionROI | null = null;

  loading = true;
  error: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(private analyticsService: SubscriptionAnalyticsService) { }

  ngOnInit(): void {
    this.loadDashboard();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadDashboard(): void {
    this.loading = true;
    this.error = null;

    this.analyticsService.getSubscriptionDashboard()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.dashboard = data;
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading subscription analytics:', err);
          this.error = 'Failed to load subscription analytics. Please try again.';
          this.loading = false;
        }
      });

    // Load additional data
    this.analyticsService.getChurnRiskAssessment()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.churnRisk = data;
        },
        error: (err) => console.error('Error loading churn risk:', err)
      });

    this.analyticsService.getUpsellOpportunities()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.upsellOpportunities = data;
        },
        error: (err) => console.error('Error loading upsell opportunities:', err)
      });

    this.analyticsService.getROIAnalysis()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.roiAnalysis = data;
        },
        error: (err) => console.error('Error loading ROI analysis:', err)
      });
  }

  getRiskLevelClass(level: string): string {
    switch (level) {
      case 'High': return 'text-red-600';
      case 'Medium': return 'text-yellow-600';
      case 'Low': return 'text-green-600';
      default: return 'text-gray-600';
    }
  }

  getRiskScoreBgClass(score: number): string {
    if (score >= 75) return 'bg-red-100';
    if (score >= 50) return 'bg-yellow-100';
    return 'bg-green-100';
  }

  getROITrendIcon(): string {
    if (this.roiAnalysis?.trend === 'improving') return '📈';
    if (this.roiAnalysis?.trend === 'declining') return '📉';
    return '➡️';
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
  }

  formatPercent(value: number): string {
    return (value * 100).toFixed(2) + '%';
  }

  downloadReport(format: 'pdf' | 'csv'): void {
    this.analyticsService.exportAnalytics(format)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `subscription-analytics.${format}`;
          link.click();
          window.URL.revokeObjectURL(url);
        },
        error: (err) => {
          console.error('Error downloading report:', err);
          this.error = 'Failed to download report. Please try again.';
        }
      });
  }
}
