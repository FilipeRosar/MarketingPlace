import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SellerCouponTableComponent } from '../seller-coupon-table/seller-coupon-table.component';
import { SellerCouponFormComponent, CouponFormData } from '../seller-coupon-form/seller-coupon-form.component';
import { CouponCodeGeneratorComponent } from '../coupon-code-generator/coupon-code-generator.component';
import { SellerCouponAnalyticsComponent } from '../seller-coupon-analytics/seller-coupon-analytics.component';
import { CouponService, Coupon, CouponAnalyticsDashboardDto, CreateCouponRequest } from '../../services/coupon/coupon.service';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-seller-coupon-management',
  standalone: true,
  imports: [
    CommonModule,
    SellerCouponTableComponent,
    SellerCouponFormComponent,
    CouponCodeGeneratorComponent,
    SellerCouponAnalyticsComponent
  ],
  template: `
    <div class="coupon-management-container">
      <!-- Tab Navigation -->
      <div class="tab-navigation">
        <button 
          *ngFor="let tab of tabs"
          [class.active]="activeTab === tab"
          (click)="onTabChange(tab)"
          class="tab-button"
        >
          {{ tab === 'list' ? '📋 Cupons' : tab === 'create' ? '✨ Criar' : tab === 'generator' ? '🎲 Gerador' : '📊 Analytics' }}
        </button>
      </div>

      <!-- Loading State -->
      <div *ngIf="isLoading" class="loading-state">
        <p>Carregando cupons...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="errorMessage" class="error-state">
        <p>⚠️ {{ errorMessage }}</p>
        <button (click)="clearError()" class="btn-dismiss">Descartar</button>
      </div>

      <!-- Tab: List Coupons -->
      <div *ngIf="activeTab === 'list' && !isLoading" class="tab-content">
        <h2>Meus Cupons</h2>
        <app-seller-coupon-table 
          [coupons]="sellerCoupons"
          (edit)="onEditCoupon($event)"
          (delete)="onDeleteCoupon($event)"
          (viewAnalytics)="onViewAnalytics($event)"
          (clone)="onCloneCoupon($event)"
        ></app-seller-coupon-table>
      </div>

      <!-- Tab: Create Coupon -->
      <div *ngIf="activeTab === 'create' && !isLoading" class="tab-content">
        <h2>{{ editingCoupon ? 'Editar Cupom' : 'Criar Novo Cupom' }}</h2>
        <app-seller-coupon-form 
          [mode]="editingCoupon ? 'edit' : 'create'"
          [initialData]="editingCoupon || undefined"
          (submitted)="onSubmitForm($event)"
          (cancelled)="onCancelForm()"
        ></app-seller-coupon-form>
      </div>

      <!-- Tab: Code Generator -->
      <div *ngIf="activeTab === 'generator' && !isLoading" class="tab-content">
        <h2>Gerador de Códigos</h2>
        <app-coupon-code-generator></app-coupon-code-generator>
      </div>

      <!-- Tab: Analytics -->
      <div *ngIf="activeTab === 'analytics'" class="tab-content">
        <h2>Analytics de Cupons</h2>
        <div *ngIf="isLoadingAnalytics" class="loading-state">
          <p>Carregando analytics...</p>
        </div>
        <div *ngIf="analyticsError" class="error-state">
          <p>⚠️ {{ analyticsError }}</p>
          <button (click)="clearAnalyticsError()" class="btn-dismiss">Descartar</button>
        </div>
        <app-seller-coupon-analytics 
          *ngIf="analyticsData && !isLoadingAnalytics"
          [analytics]="analyticsData"
        ></app-seller-coupon-analytics>
      </div>

      <!-- Success Message -->
      <div *ngIf="successMessage" class="success-state">
        <p>✓ {{ successMessage }}</p>
      </div>
    </div>
  `,
  styles: [`
    .coupon-management-container {
      padding: 20px;
      background: #f8f9fa;
      border-radius: 8px;
      min-height: 600px;
    }

    .tab-navigation {
      display: flex;
      gap: 10px;
      margin-bottom: 25px;
      border-bottom: 2px solid #e0e0e0;
      flex-wrap: wrap;
    }

    .tab-button {
      padding: 12px 20px;
      background: none;
      border: none;
      border-bottom: 3px solid transparent;
      font-size: 14px;
      font-weight: 500;
      color: #666;
      cursor: pointer;
      transition: all 0.2s;
    }

    .tab-button:hover {
      color: #333;
    }

    .tab-button.active {
      color: #0066cc;
      border-bottom-color: #0066cc;
    }

    .tab-content {
      animation: fadeIn 0.3s ease-in;
    }

    @keyframes fadeIn {
      from {
        opacity: 0;
        transform: translateY(10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    h2 {
      margin: 0 0 20px 0;
      font-size: 20px;
      color: #333;
    }

    .loading-state,
    .error-state,
    .success-state {
      padding: 20px;
      border-radius: 8px;
      text-align: center;
      margin: 20px 0;
    }

    .loading-state {
      background: #e7f3ff;
      color: #0066cc;
    }

    .error-state {
      background: #f8d7da;
      color: #721c24;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .success-state {
      background: #d4edda;
      color: #155724;
      animation: slideIn 0.3s ease-in;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .btn-dismiss {
      background: none;
      border: 1px solid #721c24;
      color: #721c24;
      padding: 5px 15px;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 500;
    }

    .btn-dismiss:hover {
      background: #721c24;
      color: white;
    }
  `]
})
export class SellerCouponManagementComponent implements OnInit {
  private couponService = inject(CouponService);
  private authService = inject(AuthService);

  tabs: Array<'list' | 'create' | 'generator' | 'analytics'> = ['list', 'create', 'generator', 'analytics'];
  activeTab: 'list' | 'create' | 'generator' | 'analytics' = 'list';

  sellerCoupons: Coupon[] = [];
  analyticsData: CouponAnalyticsDashboardDto | null = null;
  editingCoupon: CouponFormData | null = null;
  
  isLoading = false;
  isLoadingAnalytics = false;
  errorMessage = '';
  analyticsError = '';
  successMessage = '';
  
  private userId: string | null = null;

  ngOnInit() {
    this.loadSellerCoupons();
    this.obtainUserId();
  }

  private obtainUserId() {
    const currentUser = this.authService.currentUserValue;
    if (!currentUser) {
      this.errorMessage = 'Usuário não autenticado.';
      return;
    }

    this.userId = currentUser.id;
  }

  onTabChange(tab: 'list' | 'create' | 'generator' | 'analytics') {
    this.activeTab = tab;
    if (tab === 'analytics') {
      this.loadAnalytics();
    }
  }

  loadSellerCoupons() {
    this.isLoading = true;
    this.errorMessage = '';

    this.couponService.getSellerCoupons().subscribe({
      next: (coupons) => {
        console.log('✅ Cupons carregados:', coupons);
        this.sellerCoupons = coupons;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.errorMessage = 'Erro ao carregar cupons. Tente novamente.';
        this.isLoading = false;
        console.error('❌ Erro ao carregar cupons:', error);
      }
    });
  }

  loadAnalytics() {
    if (!this.userId) {
      this.analyticsError = 'ID do usuário não disponível. Tente novamente.';
      return;
    }

    this.isLoadingAnalytics = true;
    this.analyticsError = '';
    
    this.couponService.getSellerAnalyticsDashboard(this.userId).subscribe({
      next: (data) => {
        this.analyticsData = data;
        this.isLoadingAnalytics = false;
      },
      error: (error: any) => {
        this.analyticsError = 'Erro ao carregar analytics. Tente novamente.';
        this.isLoadingAnalytics = false;
        console.error(error);
      }
    });
  }

  clearAnalyticsError() {
    this.analyticsError = '';
  }

  onEditCoupon(coupon: Coupon) {
    // Mapear Coupon para CouponFormData
    this.editingCoupon = {
      code: coupon.code,
      description: coupon.description || '',
      discountType: coupon.discountType === 1 ? 'percentage' : 'fixed',
      discountValue: coupon.discountValue,
      scope: 'store', // Seria mapeado do coupon se tivesse
      validFrom: coupon.validFrom?.toISOString().split('T')[0] || '',
      validUntil: coupon.validUntil?.toISOString().split('T')[0] || '',
      usageLimit: coupon.usageLimit || 0,
      isActive: coupon.isActive
    };
    this.activeTab = 'create';
  }

  onDeleteCoupon(couponId: string) {
    if (confirm('Tem certeza que deseja deletar este cupom?')) {
      this.couponService.deleteSellerCoupon(couponId).subscribe({
        next: () => {
          this.successMessage = 'Cupom deletado com sucesso!';
          this.loadSellerCoupons();
          setTimeout(() => this.successMessage = '', 3000);
        },
        error: (error: any) => {
          this.errorMessage = 'Erro ao deletar cupom.';
          console.error(error);
        }
      });
    }
  }

  onViewAnalytics(couponId: string) {
    if (!this.userId) {
      this.errorMessage = 'ID do usuário não disponível.';
      return;
    }

    this.couponService.getCouponROI(this.userId, couponId).subscribe({
      next: () => {
        this.loadAnalytics();
        this.activeTab = 'analytics';
      },
      error: (error: any) => {
        this.errorMessage = 'Erro ao carregar analytics do cupom.';
        console.error(error);
      }
    });
  }

  onCloneCoupon(coupon: Coupon) {
    // Clonar com novo código
    const newCode = `${coupon.code}_CLONE_${Date.now()}`;
    const clonedCoupon: CreateCouponRequest = {
      ...coupon,
      code: newCode,
      isActive: false
    } as CreateCouponRequest;

    this.couponService.createSellerCoupon(clonedCoupon).subscribe({
      next: () => {
        this.successMessage = 'Cupom clonado com sucesso!';
        this.loadSellerCoupons();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        this.errorMessage = 'Erro ao clonar cupom.';
        console.error(error);
      }
    });
  }

  onSubmitForm(formData: CouponFormData) {
    const newCoupon: CreateCouponRequest = {
      code: formData.code,
      description: formData.description,
      type: 2,  // 2 = Seller
      discountType: formData.discountType === 'percentage' ? 1 : 2,  // 1 = Percentage, 2 = Fixed
      discountValue: formData.discountValue,
      usageLimit: formData.usageLimit,
      validFrom: new Date(formData.validFrom),
      validUntil: new Date(formData.validUntil),
      isActive: formData.isActive,
      minOrderValue: 0,
      scope: 1,  // 1 = EntireOrder
      usageLimitPerUser: 1,
      preventsCombination: false,
      onlyWithoutPromotion: false,
      onlyFirstPurchase: false
    };

    if (formData.scope !== 'store' && formData.scopeId) {
      if (formData.scope === 'product') {
        newCoupon.productId = formData.scopeId;
      } else {
        newCoupon.categoryId = formData.scopeId;
      }
    }

    const operation = this.editingCoupon
      ? this.couponService.updateSellerCoupon('existing-id', newCoupon)
      : this.couponService.createSellerCoupon(newCoupon);

    operation.subscribe({
      next: () => {
        this.successMessage = this.editingCoupon ? 'Cupom atualizado!' : 'Cupom criado com sucesso!';
        this.editingCoupon = null;
        this.loadSellerCoupons();
        this.activeTab = 'list';
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (error: any) => {
        this.errorMessage = 'Erro ao salvar cupom.';
        console.error(error);
      }
    });
  }

  onCancelForm() {
    this.editingCoupon = null;
    this.activeTab = 'list';
  }

  clearError() {
    this.errorMessage = '';
  }
}
