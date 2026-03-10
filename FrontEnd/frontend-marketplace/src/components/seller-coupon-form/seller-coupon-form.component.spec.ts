import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SellerCouponFormComponent, CouponFormData } from './seller-coupon-form.component';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

describe('SellerCouponFormComponent', () => {
  let component: SellerCouponFormComponent;
  let fixture: ComponentFixture<SellerCouponFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SellerCouponFormComponent, CommonModule, ReactiveFormsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(SellerCouponFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with default values in create mode', () => {
    expect(component.form).toBeDefined();
    expect(component.form.get('code')?.value).toBe('');
    expect(component.form.get('discountType')?.value).toBe('percentage');
    expect(component.form.get('scope')?.value).toBe('store');
  });

  it('should initialize form with provided data in edit mode', () => {
    const initialData: CouponFormData = {
      code: 'SUMMER2024',
      description: 'Summer Promo',
      discountType: 'percentage',
      discountValue: 15,
      scope: 'store',
      validFrom: '2024-06-01',
      validUntil: '2024-08-31',
      usageLimit: 100,
      isActive: true
    };

    component.mode = 'edit';
    component.initialData = initialData;
    component.ngOnInit();

    expect(component.form.get('code')?.value).toBe('SUMMER2024');
    expect(component.form.get('discountValue')?.value).toBe(15);
  });

  it('should validate required fields', () => {
    expect(component.form.valid).toBeFalsy();

    component.form.patchValue({
      code: 'TEST123',
      description: 'Test Coupon',
      discountType: 'percentage',
      discountValue: 10,
      validFrom: '2024-06-01',
      validUntil: '2024-08-31',
      usageLimit: 100
    });

    expect(component.form.valid).toBeTruthy();
  });

  it('should validate minimum code length', () => {
    const codeControl = component.form.get('code');
    codeControl?.setValue('AB');
    expect(codeControl?.hasError('minlength')).toBeTruthy();

    codeControl?.setValue('ABC');
    expect(codeControl?.hasError('minlength')).toBeFalsy();
  });

  it('should validate discount value is positive', () => {
    const discountControl = component.form.get('discountValue');
    discountControl?.setValue(0);
    expect(discountControl?.hasError('min')).toBeTruthy();

    discountControl?.setValue(10);
    expect(discountControl?.hasError('min')).toBeFalsy();
  });

  it('should validate usage limit is positive', () => {
    const limitControl = component.form.get('usageLimit');
    limitControl?.setValue(0);
    expect(limitControl?.hasError('min')).toBeTruthy();

    limitControl?.setValue(50);
    expect(limitControl?.hasError('min')).toBeFalsy();
  });

  it('should require scopeId when scope is not store', (done) => {
    component.form.patchValue({
      scope: 'product',
      scopeId: ''
    });

    setTimeout(() => {
      const scopeIdControl = component.form.get('scopeId');
      expect(scopeIdControl?.hasError('required')).toBeTruthy();
      done();
    }, 100);
  });

  it('should not require scopeId when scope is store', (done) => {
    component.form.patchValue({
      scope: 'store',
      scopeId: ''
    });

    setTimeout(() => {
      const scopeIdControl = component.form.get('scopeId');
      expect(scopeIdControl?.valid).toBeTruthy();
      done();
    }, 100);
  });

  it('should emit submitted event with form data', (done) => {
    const formData: CouponFormData = {
      code: 'TEST123',
      description: 'Test Coupon',
      discountType: 'percentage',
      discountValue: 10,
      scope: 'store',
      validFrom: '2024-06-01',
      validUntil: '2024-08-31',
      usageLimit: 100,
      isActive: true
    };

    component.submitted.subscribe((data: CouponFormData) => {
      expect(data.code).toBe('TEST123');
      expect(data.discountValue).toBe(10);
      done();
    });

    component.form.patchValue(formData);
    component.onSubmit();
  });

  it('should emit cancelled event', (done) => {
    component.cancelled.subscribe(() => {
      expect(true).toBeTruthy();
      done();
    });

    component.onCancel();
  });

  it('should not submit if form is invalid', () => {
    let submitted = false;
    component.submitted.subscribe(() => {
      submitted = true;
    });

    component.form.patchValue({
      code: 'AB' // Too short
    });
    component.onSubmit();

    expect(submitted).toBeFalsy();
  });

  it('should identify invalid fields correctly', () => {
    const codeControl = component.form.get('code');
    codeControl?.setValue('');
    codeControl?.markAsTouched();

    expect(component.isFieldInvalid('code')).toBeTruthy();

    codeControl?.setValue('VALID');
    expect(component.isFieldInvalid('code')).toBeFalsy();
  });

  it('should toggle discount type between percentage and fixed', () => {
    component.form.patchValue({
      discountType: 'percentage',
      discountValue: 10
    });
    expect(component.form.get('discountType')?.value).toBe('percentage');

    component.form.patchValue({
      discountType: 'fixed'
    });
    expect(component.form.get('discountType')?.value).toBe('fixed');
  });
});
