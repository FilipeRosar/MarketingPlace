import { Component, OnInit, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SellerAnalyticsService, AdvancedAnalyticsDashboard } from '../../services/analytics/seller-analytics.service';
import { SellerSubscription, SellerPlan } from '../../services/seller/seller.service';
import { LoadingSpinnerComponent } from '../loading-spinner.component/loading-spinner.component';

@Component({
  selector: 'app-seller-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinnerComponent],
  templateUrl: './seller-analytics.html',
  styleUrl: './seller-analytics.css'
})
export class SellerAnalyticsComponent implements OnInit {
  private analyticsService = inject(SellerAnalyticsService);

  @Input() subscription: SellerSubscription | null = null;

  isLoading = true;
  errorMessage: string | null = null;

  // Subscription flags
  isPro = false;
  isPremium = false;

  // Analytics Data
  analyticsData: AdvancedAnalyticsDashboard | null = null;

  // Period selection
  selectedPeriod: 'week' | 'month' | 'quarter' = 'month';

  ngOnInit() {
    // Set plan flags from input subscription
    if (this.subscription) {
      this.isPro = this.subscription.plan === 'Pro';
      this.isPremium = this.subscription.plan === 'Premium';
      this.loadAnalytics();
    } else {
      this.errorMessage = 'Assinatura não disponível.';
      this.isLoading = false;
    }
  }

  loadAnalytics() {
    this.isLoading = true;
    this.errorMessage = null;
    
    const days = this.selectedPeriod === 'week' ? 7 : this.selectedPeriod === 'month' ? 30 : 90;

    this.analyticsService.getAdvancedDashboard(days).subscribe({
      next: (data) => {
        this.analyticsData = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erro ao carregar analytics:', err);
        this.isLoading = false;
        this.errorMessage = 'Erro ao carregar dados de analytics.';
      }
    });
  }

  changePeriod(period: 'week' | 'month' | 'quarter') {
    this.selectedPeriod = period;
    this.loadAnalytics();
  }

  formatCurrency(value: number | undefined): string {
    if (!value) return 'R$ 0,00';
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(value);
  }

  formatPercent(value: number | undefined): string {
    if (!value) return '0%';
    return `${(value * 100).toFixed(2)}%`;
  }
}
