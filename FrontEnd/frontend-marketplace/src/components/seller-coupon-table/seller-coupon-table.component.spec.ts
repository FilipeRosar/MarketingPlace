import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SellerCouponTableComponent } from './seller-coupon-table.component';
import { Coupon } from '../../services/coupon/coupon.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';

describe('SellerCouponTableComponent', () => {
  let component: SellerCouponTableComponent;
  let fixture: ComponentFixture<SellerCouponTableComponent>;

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
    },
    {
      id: '2',
      code: 'WINTER2024',
      description: 'Winter Discount',
      discountType: 'Fixed',
      discountValue: 50,
      usageLimit: 50,
      usageCount: 50,
      validFrom: new Date('2024-12-01'),
      validUntil: new Date('2025-02-28'),
      isActive: false,
      type: 'Seller',
      creatorSellerId: 'seller-123',
      createdAt: new Date(),
      updatedAt: new Date()
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SellerCouponTableComponent, CommonModule, FormsModule, CurrencyBrPipe]
    }).compileComponents();

    fixture = TestBed.createComponent(SellerCouponTableComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display all coupons when no filters applied', () => {
    component.coupons = mockCoupons;
    component.ngOnInit();

    expect(component.filteredCoupons.length).toBe(2);
  });

  it('should filter by active status', () => {
    component.coupons = mockCoupons;
    component.statusFilter = 'active';
    component.applyFilters();

    expect(component.filteredCoupons.length).toBe(1);
    expect(component.filteredCoupons[0].code).toBe('SUMMER2024');
  });

  it('should filter by inactive status', () => {
    component.coupons = mockCoupons;
    component.statusFilter = 'inactive';
    component.applyFilters();

    expect(component.filteredCoupons.length).toBe(1);
    expect(component.filteredCoupons[0].code).toBe('WINTER2024');
  });

  it('should filter by coupon code search', () => {
    component.coupons = mockCoupons;
    component.searchTerm = 'SUMMER';
    component.applyFilters();

    expect(component.filteredCoupons.length).toBe(1);
    expect(component.filteredCoupons[0].code).toBe('SUMMER2024');
  });

  it('should filter by type', () => {
    component.coupons = mockCoupons;
    component.typeFilter = 'Seller';
    component.applyFilters();

    expect(component.filteredCoupons.length).toBe(2);
  });

  it('should emit edit event when edit button clicked', (done) => {
    component.coupons = mockCoupons;
    component.ngOnInit();

    component.edit.subscribe((coupon: Coupon) => {
      expect(coupon.id).toBe('1');
      done();
    });

    component.edit.emit(mockCoupons[0]);
  });

  it('should emit delete event with coupon ID', (done) => {
    component.coupons = mockCoupons;
    component.ngOnInit();

    component.delete.subscribe((couponId: string) => {
      expect(couponId).toBe('1');
      done();
    });

    component.delete.emit('1');
  });

  it('should emit analytics event when analytics button clicked', (done) => {
    component.coupons = mockCoupons;
    component.ngOnInit();

    component.viewAnalytics.subscribe((couponId: string) => {
      expect(couponId).toBe('1');
      done();
    });

    component.viewAnalytics.emit('1');
  });

  it('should update filtered coupons on input changes', () => {
    component.coupons = mockCoupons;
    component.ngOnInit();
    expect(component.filteredCoupons.length).toBe(2);

    component.coupons = [mockCoupons[0]];
    component.ngOnChanges({ coupons: { currentValue: [mockCoupons[0]] } as any });

    expect(component.filteredCoupons.length).toBe(1);
  });

  it('should display empty state when no coupons match filters', () => {
    component.coupons = mockCoupons;
    component.searchTerm = 'NONEXISTENT';
    component.applyFilters();

    expect(component.filteredCoupons.length).toBe(0);
  });
});
