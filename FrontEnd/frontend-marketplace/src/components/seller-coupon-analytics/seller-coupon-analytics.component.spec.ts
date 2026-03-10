import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SellerCouponAnalyticsComponent } from './seller-coupon-analytics.component';
import { CommonModule } from '@angular/common';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';
import { CouponAnalyticsDashboardDto, CouponPerformanceComparisonDto } from '../../services/coupon/coupon.service';

describe('SellerCouponAnalyticsComponent', () => {
  let component: SellerCouponAnalyticsComponent;
  let fixture: ComponentFixture<SellerCouponAnalyticsComponent>;

  const mockAnalytics: CouponAnalyticsDashboardDto = {
    totalSavedByCustomers: 1500.50,
    activeCouponsCount: 5,
    averageROI: 125.75,
    conversionRate: 45.5,
    monthlyTrend: 15.2,
    topPerformers: [
      {
        id: '1',
        code: 'SUMMER2024',
        roi: 250,
        usages: 50,
        conversionRate: 75,
        avgOrderValue: 150.00
      },
      {
        id: '2',
        code: 'SPRING2024',
        roi: 180,
        usages: 35,
        conversionRate: 65,
        avgOrderValue: 120.00
      }
    ],
    bottomPerformers: [
      {
        id: '3',
        code: 'WINTER2024',
        roi: 10,
        usages: 5,
        conversionRate: 15,
        avgOrderValue: 80.00
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SellerCouponAnalyticsComponent, CommonModule, CurrencyBrPipe]
    }).compileComponents();

    fixture = TestBed.createComponent(SellerCouponAnalyticsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display summary cards with analytics data', () => {
    component.analytics = mockAnalytics;
    fixture.detectChanges();

    expect(component.analytics.totalSavedByCustomers).toBe(1500.50);
    expect(component.analytics.activeCouponsCount).toBe(5);
    expect(component.analytics.averageROI).toBe(125.75);
    expect(component.analytics.conversionRate).toBe(45.5);
  });

  it('should display top performers table', () => {
    component.analytics = mockAnalytics;
    fixture.detectChanges();

    expect(component.analytics.topPerformers.length).toBe(2);
    expect(component.analytics.topPerformers[0].code).toBe('SUMMER2024');
    expect(component.analytics.topPerformers[0].roi).toBe(250);
  });

  it('should display bottom performers table', () => {
    component.analytics = mockAnalytics;
    fixture.detectChanges();

    expect(component.analytics.bottomPerformers.length).toBe(1);
    expect(component.analytics.bottomPerformers[0].code).toBe('WINTER2024');
    expect(component.analytics.bottomPerformers[0].roi).toBe(10);
  });

  it('should display trend indicator as positive', () => {
    component.analytics = mockAnalytics;
    fixture.detectChanges();

    expect(component.analytics.monthlyTrend).toBe(15.2);
    expect(component.analytics.monthlyTrend > 0).toBeTruthy();
  });

  it('should display trend indicator as negative', () => {
    const negativeTrendAnalytics: CouponAnalyticsDashboardDto = {
      ...mockAnalytics,
      monthlyTrend: -10.5
    };

    component.analytics = negativeTrendAnalytics;
    fixture.detectChanges();

    expect(component.analytics.monthlyTrend < 0).toBeTruthy();
  });

  it('should display empty state for top performers', () => {
    const emptyAnalytics: CouponAnalyticsDashboardDto = {
      ...mockAnalytics,
      topPerformers: []
    };

    component.analytics = emptyAnalytics;
    fixture.detectChanges();

    expect(component.analytics.topPerformers.length).toBe(0);
  });

  it('should display empty state for bottom performers', () => {
    const emptyAnalytics: CouponAnalyticsDashboardDto = {
      ...mockAnalytics,
      bottomPerformers: []
    };

    component.analytics = emptyAnalytics;
    fixture.detectChanges();

    expect(component.analytics.bottomPerformers.length).toBe(0);
  });

  it('should handle zero values correctly', () => {
    const zeroAnalytics: CouponAnalyticsDashboardDto = {
      totalSavedByCustomers: 0,
      activeCouponsCount: 0,
      averageROI: 0,
      conversionRate: 0,
      monthlyTrend: 0,
      topPerformers: [],
      bottomPerformers: []
    };

    component.analytics = zeroAnalytics;
    fixture.detectChanges();

    expect(component.analytics.totalSavedByCustomers).toBe(0);
    expect(component.analytics.activeCouponsCount).toBe(0);
  });

  it('should display all performance metrics in tables', () => {
    component.analytics = mockAnalytics;
    fixture.detectChanges();

    const topPerformer = component.analytics.topPerformers[0];
    expect(topPerformer.code).toBeDefined();
    expect(topPerformer.roi).toBeDefined();
    expect(topPerformer.usages).toBeDefined();
    expect(topPerformer.conversionRate).toBeDefined();
    expect(topPerformer.avgOrderValue).toBeDefined();
  });

  it('should have correct component properties initialized', () => {
    fixture.detectChanges();
    expect(component.analytics).toBeDefined();
  });
});
