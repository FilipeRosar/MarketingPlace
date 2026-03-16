import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SellerAnalyticsComponent } from './seller-analytics.component';
import { SellerAnalyticsService } from '../../services/analytics/seller-analytics.service';
import { SellerSubscriptionService } from '../../services/subscription/seller-subscription.service';
import { of, throwError } from 'rxjs';

describe('SellerAnalyticsComponent', () => {
  let component: SellerAnalyticsComponent;
  let fixture: ComponentFixture<SellerAnalyticsComponent>;
  let analyticsService: jasmine.SpyObj<SellerAnalyticsService>;
  let subscriptionService: jasmine.SpyObj<SellerSubscriptionService>;

  beforeEach(async () => {
    const analyticsServiceSpy = jasmine.createSpyObj('SellerAnalyticsService', [
      'getAdvancedAnalyticsDashboard',
      'getBasicAnalyticsDashboard'
    ]);
    const subscriptionServiceSpy = jasmine.createSpyObj('SellerSubscriptionService', ['getSubscription']);

    await TestBed.configureTestingModule({
      imports: [SellerAnalyticsComponent],
      providers: [
        { provide: SellerAnalyticsService, useValue: analyticsServiceSpy },
        { provide: SellerSubscriptionService, useValue: subscriptionServiceSpy }
      ]
    }).compileComponents();

    analyticsService = TestBed.inject(SellerAnalyticsService) as jasmine.SpyObj<SellerAnalyticsService>;
    subscriptionService = TestBed.inject(SellerSubscriptionService) as jasmine.SpyObj<SellerSubscriptionService>;
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SellerAnalyticsComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
