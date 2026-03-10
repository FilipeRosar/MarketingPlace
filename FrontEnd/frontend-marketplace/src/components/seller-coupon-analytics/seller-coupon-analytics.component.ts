import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CouponAnalyticsDashboardDto, CouponPerformanceComparisonDto } from '../../services/coupon/coupon.service';

@Component({
  selector: 'app-seller-coupon-analytics',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="analytics-container">
      <!-- Summary Cards -->
      <div class="summary-cards">
        <div class="card">
          <div class="card-label">Total Economizado</div>
          <div class="card-value">R$ {{ analytics.totalSavedByCustomers | number: '1.2-2' }}</div>
        </div>
        <div class="card">
          <div class="card-label">Cupons Ativos</div>
          <div class="card-value">{{ analytics.activeCouponsCount }}</div>
        </div>
        <div class="card">
          <div class="card-label">ROI Médio</div>
          <div class="card-value">{{ analytics.averageROI | number: '1.0-0' }}%</div>
        </div>
        <div class="card">
          <div class="card-label">Conversão Média</div>
          <div class="card-value">{{ analytics.conversionRate | number: '1.1-1' }}%</div>
        </div>
        <div class="card" [class.trend-positive]="analytics.monthlyTrend > 0" [class.trend-negative]="analytics.monthlyTrend < 0">
          <div class="card-label">Trend. Mensal</div>
          <div class="card-value">
            <span *ngIf="analytics.monthlyTrend > 0">↑ {{ analytics.monthlyTrend | number: '1.1-1' }}%</span>
            <span *ngIf="analytics.monthlyTrend < 0">↓ {{ analytics.monthlyTrend | number: '1.1-1' }}%</span>
            <span *ngIf="analytics.monthlyTrend === 0">— 0%</span>
          </div>
        </div>
      </div>

      <!-- Top Performers -->
      <div class="performers-section">
        <h3>🏆 Top Performers (ROI)</h3>
        <div class="performers-table">
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Código</th>
                <th>ROI</th>
                <th>Usos</th>
                <th>Conversão</th>
                <th>Ticket Médio</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let coupon of analytics.topPerformers; let i = index">
                <td>{{ i + 1 }}</td>
                <td><strong>{{ coupon.code }}</strong></td>
                <td class="highlight-positive">{{ coupon.roi | number: '1.0-0' }}%</td>
                <td>{{ coupon.usages }}</td>
                <td>{{ coupon.conversionRate | number: '1.1-1' }}%</td>
                <td>R$ {{ coupon.avgOrderValue | number: '1.2-2' }}</td>
              </tr>
              <tr *ngIf="analytics.topPerformers.length === 0">
                <td colspan="6" class="empty">Sem dados</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Bottom Performers -->
      <div class="performers-section">
        <h3>📉 Precisa Melhorar (Menor ROI)</h3>
        <div class="performers-table">
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Código</th>
                <th>ROI</th>
                <th>Usos</th>
                <th>Conversão</th>
                <th>Ticket Médio</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let coupon of analytics.bottomPerformers; let i = index">
                <td>{{ i + 1 }}</td>
                <td><strong>{{ coupon.code }}</strong></td>
                <td class="highlight-negative">{{ coupon.roi | number: '1.0-0' }}%</td>
                <td>{{ coupon.usages }}</td>
                <td>{{ coupon.conversionRate | number: '1.1-1' }}%</td>
                <td>R$ {{ coupon.avgOrderValue | number: '1.2-2' }}</td>
              </tr>
              <tr *ngIf="analytics.bottomPerformers.length === 0">
                <td colspan="6" class="empty">Sem dados</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .analytics-container {
      padding: 20px;
      background: #f8f9fa;
      border-radius: 8px;
    }

    .summary-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 15px;
      margin-bottom: 30px;
    }

    .card {
      background: white;
      padding: 20px;
      border-radius: 8px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      text-align: center;
    }

    .card-label {
      font-size: 12px;
      color: #666;
      margin-bottom: 8px;
      text-transform: uppercase;
      font-weight: 500;
    }

    .card-value {
      font-size: 24px;
      font-weight: 700;
      color: #333;
    }

    .card.trend-positive {
      border-left: 4px solid #28a745;
    }

    .card.trend-negative {
      border-left: 4px solid #dc3545;
    }

    .performers-section {
      margin-bottom: 30px;
      background: white;
      padding: 20px;
      border-radius: 8px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    .performers-section h3 {
      margin: 0 0 15px 0;
      font-size: 16px;
      color: #333;
    }

    .performers-table {
      overflow-x: auto;
    }

    table {
      width: 100%;
      border-collapse: collapse;
    }

    thead {
      background: #f0f0f0;
      font-weight: 600;
      color: #333;
    }

    th, td {
      padding: 12px 15px;
      text-align: left;
      border-bottom: 1px solid #e0e0e0;
      font-size: 14px;
    }

    tbody tr:hover {
      background: #fafafa;
    }

    .empty {
      text-align: center;
      color: #999;
    }

    .highlight-positive {
      color: #28a745;
      font-weight: 600;
    }

    .highlight-negative {
      color: #dc3545;
      font-weight: 600;
    }
  `]
})
export class SellerCouponAnalyticsComponent implements OnInit {
  @Input() analytics!: CouponAnalyticsDashboardDto;

  ngOnInit() {
    // Component initialization if needed
  }
}
