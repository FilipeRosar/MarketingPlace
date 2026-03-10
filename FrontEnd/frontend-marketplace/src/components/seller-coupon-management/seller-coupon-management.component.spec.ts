import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SellerCouponManagementComponent } from './seller-coupon-management.component';
import { CouponService, Coupon, CouponAnalyticsDashboardDto } from '../../services/coupon/coupon.service';
import { of, throwError } from 'rxjs';
import { CommonModule } from '@angular/common';

describe('SellerCouponManagementComponent', () => {
  let component: SellerCouponManagementComponent;
  let fixture: ComponentFixture<SellerCouponManagementComponent>;
  let couponService: jasmine.SpyObj<CouponService>;

  const mockCoupons: Coupon[] = [
    {
      id: '1',
      code: 'SUMMER2024',
      description: 'Summer Promotion',
      discountType: 'Percentage',
      discountValue: 10,
      usageLimit: 100,
      usageCount: 25,
      validFrom: new Date('2024-06-01'),
      validUntil: new Date('2024-08-31'),
      isActive: true,
      type: 'Seller',
      creatorSellerId: 'seller-123',
      createdAt: new Date(),
      updatedAt: new Date()
    }
  ];

  const mockAnalytics: CouponAnalyticsDashboardDto = {
    totalSavedByCustomers: 1500,
    activeCouponsCount: 5,
    averageROI: 125,
    conversionRate: 45,
    monthlyTrend: 15,
    topPerformers: [],
    bottomPerformers: []
  };

  beforeEach(async () => {
    const couponServiceSpy = jasmine.createSpyObj('CouponService', [
      'getSellerCoupons',
      'createCoupon',
      'updateCoupon',
      'deleteCoupon',
      'getCouponAnalytics',
      'getSellerAnalyticsDashboard'
    ]);

    await TestBed.configureTestingModule({
      imports: [SellerCouponManagementComponent, CommonModule],
      providers: [
        { provide: CouponService, useValue: couponServiceSpy }
      ]
    }).compileComponents();

    couponService = TestBed.inject(CouponService) as jasmine.SpyObj<CouponService>;
    couponService.getSellerCoupons.and.returnValue(of(mockCoupons));

    fixture = TestBed.createComponent(SellerCouponManagementComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load seller coupons on init', () => {
    fixture.detectChanges();

    expect(couponService.getSellerCoupons).toHaveBeenCalled();
    expect(component.sellerCoupons.length).toBe(1);
  });

  it('should handle error loading coupons', () => {
    couponService.getSellerCoupons.and.returnValue(throwError(() => new Error('API Error')));

    fixture.detectChanges();

    expect(component.errorMessage).toContain('Erro ao carregar cupons');
  });

  it('should switch to create tab', () => {
    component.activeTab = 'list';
    component.activeTab = 'create';

    expect(component.activeTab).toBe('create');
  });

  it('should switch to analytics tab', () => {
    component.activeTab = 'list';
    component.activeTab = 'analytics';

    expect(component.activeTab).toBe('analytics');
  });

  it('should load analytics data', () => {
    couponService.getSellerAnalyticsDashboard.and.returnValue(of(mockAnalytics));

    component.loadAnalytics();

    expect(couponService.getSellerAnalyticsDashboard).toHaveBeenCalled();
    expect(component.analyticsData).toEqual(mockAnalytics);
  });

  it('should delete coupon and reload list', () => {
    couponService.deleteCoupon.and.returnValue(of(undefined));
    couponService.getSellerCoupons.and.returnValue(of([]));

    spyOn(window, 'confirm').and.returnValue(true);

    component.onDeleteCoupon('1');

    expect(couponService.deleteCoupon).toHaveBeenCalledWith('1');
    expect(component.successMessage).toContain('deletado');
  });

  it('should not delete coupon if user cancels', () => {
    spyOn(window, 'confirm').and.returnValue(false);

    component.onDeleteCoupon('1');

    expect(couponService.deleteCoupon).not.toHaveBeenCalled();
  });

  it('should create new coupon', () => {
    couponService.createCoupon.and.returnValue(of(undefined));
    couponService.getSellerCoupons.and.returnValue(of(mockCoupons));

    const formData = {
      code: 'NEW123',
      description: 'New Coupon',
      discountType: 'percentage' as const,
      discountValue: 15,
      scope: 'store' as const,
      validFrom: '2024-06-01',
      validUntil: '2024-08-31',
      usageLimit: 100,
      isActive: true
    };

    component.onSubmitForm(formData);

    expect(couponService.createCoupon).toHaveBeenCalled();
    expect(component.successMessage).toContain('criado');
  });

  it('should update existing coupon', () => {
    couponService.updateCoupon.and.returnValue(of(undefined));
    couponService.getSellerCoupons.and.returnValue(of(mockCoupons));

    component.editingCoupon = {
      code: 'EDITED',
      description: 'Edited Coupon',
      discountType: 'percentage' as const,
      discountValue: 20,
      scope: 'store' as const,
      validFrom: '2024-06-01',
      validUntil: '2024-08-31',
      usageLimit: 100,
      isActive: true
    };

    const formData = { ...component.editingCoupon };
    component.onSubmitForm(formData);

    expect(couponService.updateCoupon).toHaveBeenCalled();
    expect(component.successMessage).toContain('atualizado');
  });

  it('should clone coupon', () => {
    couponService.createCoupon.and.returnValue(of(undefined));
    couponService.getSellerCoupons.and.returnValue(of(mockCoupons));

    component.onCloneCoupon(mockCoupons[0]);

    expect(couponService.createCoupon).toHaveBeenCalled();
    expect(component.successMessage).toContain('clonado');
  });

  it('should cancel form editing', () => {
    component.editingCoupon = { code: 'TEST' } as any;
    component.activeTab = 'create';

    component.onCancelForm();

    expect(component.editingCoupon).toBeNull();
    expect(component.activeTab).toBe('list');
  });

  it('should clear error message', () => {
    component.errorMessage = 'Test error';
    component.clearError();

    expect(component.errorMessage).toBe('');
  });

  it('should show loading state on init', () => {
    expect(component.isLoading).toBeTruthy();
    fixture.detectChanges();
  });

  it('should hide loading state after loading coupons', (done) => {
    fixture.detectChanges();

    setTimeout(() => {
      expect(component.isLoading).toBeFalsy();
      done();
    }, 100);
  });

  it('should view analytics for specific coupon', () => {
    couponService.getCouponAnalytics.and.returnValue(of(undefined));
    couponService.getSellerAnalyticsDashboard.and.returnValue(of(mockAnalytics));

    component.onViewAnalytics('1');

    expect(couponService.getCouponAnalytics).toHaveBeenCalledWith('seller-123', '1');
    expect(component.activeTab).toBe('analytics');
  });

  it('should set all tabs available', () => {
    expect(component.tabs).toContain('list');
    expect(component.tabs).toContain('create');
    expect(component.tabs).toContain('generator');
    expect(component.tabs).toContain('analytics');
  });
});
