import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CouponService, Coupon } from '../../../services/coupon/coupon.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-coupon-management',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="coupon-management">
      <div class="header">
        <h2>Gerenciamento de Cupons</h2>
        <button class="btn-primary" (click)="openCreateModal()">+ Novo Cupom</button>
      </div>

      <!-- Filtros -->
      <div class="filters">
        <select [(ngModel)]="selectedType" (change)="loadCoupons()" class="filter-select">
          <option value="">Todos os Tipos</option>
          <option value="0">Platform</option>
          <option value="1">Seller</option>
          <option value="2">Intelligent</option>
          <option value="3">PlanBased</option>
        </select>
      </div>

      <!-- Tabela de Cupons -->
      <div class="table-container" *ngIf="coupons.length > 0; else emptyState">
        <table class="coupons-table">
          <thead>
            <tr>
              <th>Código</th>
              <th>Tipo</th>
              <th>Desconto</th>
              <th>Vendedor</th>
              <th>Uso</th>
              <th>Válido até</th>
              <th>Status</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let coupon of coupons" [class.inactive]="!coupon.isActive">
              <td><strong>{{ coupon.code }}</strong></td>
              <td>
                <span class="badge" [ngClass]="'badge-' + getCouponTypeClass(coupon.type)">
                  {{ getCouponTypeName(coupon.type) }}
                </span>
              </td>
              <td>
                <span *ngIf="coupon.discountType === 1">
                  {{ coupon.discountValue }}%
                  <span *ngIf="coupon.maxDiscount">(Máx: R$ {{ coupon.maxDiscount | number:'1.2-2' }})</span>
                </span>
                <span *ngIf="coupon.discountType === 2">
                  R$ {{ coupon.discountValue | number:'1.2-2' }}
                </span>
              </td>
              <td>
                <span *ngIf="coupon.creatorSellerName">{{ coupon.creatorSellerName }}</span>
                <span *ngIf="!coupon.creatorSellerName">Plataforma</span>
              </td>
              <td>
                {{ coupon.usageCount }}<span *ngIf="coupon.usageLimit > 0"> / {{ coupon.usageLimit }}</span>
              </td>
              <td>{{ coupon.validUntil | date:'short' }}</td>
              <td>
                <span class="badge" [ngClass]="coupon.isActive ? 'badge-success' : 'badge-danger'">
                  {{ coupon.isActive ? 'Ativo' : 'Inativo' }}
                </span>
              </td>
              <td class="actions">
                <button class="btn-sm btn-info" (click)="viewDetails(coupon)">Detalhes</button>
                <button class="btn-sm btn-warning" (click)="editCoupon(coupon)">Editar</button>
                <button class="btn-sm btn-danger" (click)="deleteCoupon(coupon.id)">Deletar</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <ng-template #emptyState>
        <div class="empty-state">
          <p>Nenhum cupom encontrado</p>
        </div>
      </ng-template>

      <!-- Modal de Criação/Edição -->
      <div class="modal" *ngIf="showModal">
        <div class="modal-content">
          <div class="modal-header">
            <h3>{{ editingCoupon ? 'Editar Cupom' : 'Criar Novo Cupom' }}</h3>
            <button class="btn-close" (click)="closeModal()">×</button>
          </div>

          <form [formGroup]="couponForm" (ngSubmit)="saveCoupon()" class="modal-body">
            <div class="form-group">
              <label>Código *</label>
              <input type="text" formControlName="code" class="form-control" placeholder="Ex: BEMVINDO15" 
                     [readonly]="!!editingCoupon">
            </div>

            <div class="form-group">
              <label>Descrição</label>
              <textarea formControlName="description" class="form-control" 
                        placeholder="Descrição do cupom"></textarea>
            </div>

            <div class="form-row">
              <div class="form-group">
                <label>Tipo *</label>
                <select formControlName="type" class="form-control" [disabled]="!!editingCoupon">
                  <option [value]="1">Platform</option>
                  <option [value]="2">Seller</option>
                  <option [value]="3">Intelligent</option>
                  <option [value]="4">PlanBased</option>
                </select>
              </div>

              <div class="form-group">
                <label>Tipo de Desconto *</label>
                <select formControlName="discountType" class="form-control">
                  <option [value]="1">Percentual (%)</option>
                  <option [value]="2">Fixo (R$)</option>
                </select>
              </div>
            </div>

            <div class="form-row">
              <div class="form-group">
                <label>Valor do Desconto *</label>
                <input type="number" formControlName="discountValue" class="form-control" 
                       placeholder="Ex: 15" step="0.01">
              </div>

              <div class="form-group" *ngIf="couponForm.get('discountType')?.value === 1">
                <label>Desconto Máximo</label>
                <input type="number" formControlName="maxDiscount" class="form-control" 
                       placeholder="Ex: 50" step="0.01">
              </div>
            </div>

            <div class="form-group">
              <label>Valor Mínimo de Compra</label>
              <input type="number" formControlName="minOrderValue" class="form-control" 
                     placeholder="Ex: 100" step="0.01">
            </div>

            <div class="form-row">
              <div class="form-group">
                <label>Data Inicial *</label>
                <input type="date" formControlName="validFrom" class="form-control">
              </div>

              <div class="form-group">
                <label>Data Final *</label>
                <input type="date" formControlName="validUntil" class="form-control">
              </div>
            </div>

            <div class="form-row">
              <div class="form-group">
                <label>Limite de Uso Total</label>
                <input type="number" formControlName="usageLimit" class="form-control" 
                       placeholder="0 = ilimitado">
              </div>

              <div class="form-group">
                <label>Limite por Usuário</label>
                <input type="number" formControlName="usageLimitPerUser" class="form-control" 
                       placeholder="Ex: 1" min="1">
              </div>
            </div>

            <div class="form-checkboxes">
              <label class="checkbox">
                <input type="checkbox" formControlName="isActive">
                <span>Ativo</span>
              </label>
              <label class="checkbox">
                <input type="checkbox" formControlName="preventsCombination">
                <span>Previne Combinação com Outros Cupons</span>
              </label>
              <label class="checkbox">
                <input type="checkbox" formControlName="onlyFirstPurchase">
                <span>Apenas Primeira Compra</span>
              </label>
              <label class="checkbox">
                <input type="checkbox" formControlName="onlyWithoutPromotion">
                <span>Apenas Produtos sem Promoção</span>
              </label>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn-secondary" (click)="closeModal()">Cancelar</button>
              <button type="submit" class="btn-primary" [disabled]="!couponForm.valid || loading">
                {{ loading ? 'Salvando...' : 'Salvar' }}
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Modal de Detalhes -->
      <div class="modal" *ngIf="showDetailsModal && selectedCoupon">
        <div class="modal-content modal-details">
          <div class="modal-header">
            <h3>Detalhes do Cupom: {{ selectedCoupon.code }}</h3>
            <button class="btn-close" (click)="closeDetailsModal()">×</button>
          </div>

          <div class="modal-body">
            <div class="details-grid">
              <div class="detail-item">
                <label>Código</label>
                <p>{{ selectedCoupon.code }}</p>
              </div>
              <div class="detail-item">
                <label>Tipo</label>
                <p>{{ getCouponTypeName(selectedCoupon.type) }}</p>
              </div>
              <div class="detail-item">
                <label>Desconto</label>
                <p>
                  <span *ngIf="selectedCoupon.discountType === 1">
                    {{ selectedCoupon.discountValue }}%
                    <span *ngIf="selectedCoupon.maxDiscount">(Máx: R$ {{ selectedCoupon.maxDiscount }})</span>
                  </span>
                  <span *ngIf="selectedCoupon.discountType === 2">
                    R$ {{ selectedCoupon.discountValue | number:'1.2-2' }}
                  </span>
                </p>
              </div>
              <div class="detail-item">
                <label>Vendedor</label>
                <p>
                  <span *ngIf="selectedCoupon.creatorSellerName">{{ selectedCoupon.creatorSellerName }}</span>
                  <span *ngIf="!selectedCoupon.creatorSellerName">Plataforma</span>
                </p>
              </div>
              <div class="detail-item">
                <label>Usos</label>
                <p>{{ selectedCoupon.usageCount }}<span *ngIf="selectedCoupon.usageLimit > 0"> / {{ selectedCoupon.usageLimit }}</span></p>
              </div>
              <div class="detail-item">
                <label>Válido de</label>
                <p>{{ selectedCoupon.validFrom | date:'short' }} até {{ selectedCoupon.validUntil | date:'short' }}</p>
              </div>
            </div>

            <div class="usage-history" *ngIf="couponUsage">
              <h4>Histórico de Uso</h4>
              <div *ngIf="couponUsage.recentUses.length > 0; else noUsage">
                <div class="usage-item" *ngFor="let use of couponUsage.recentUses">
                  <span class="user">{{ use.userName }}</span>
                  <span class="amount">-R$ {{ use.discountApplied | number:'1.2-2' }}</span>
                  <span class="date">{{ use.usedAt | date:'short' }}</span>
                </div>
              </div>
              <ng-template #noUsage>
                <p>Nenhum uso registrado</p>
              </ng-template>
            </div>
          </div>

          <div class="modal-footer">
            <button class="btn-secondary" (click)="closeDetailsModal()">Fechar</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .coupon-management {
      padding: 20px;
      background: #f5f5f5;
      border-radius: 8px;
    }

    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .header h2 {
      margin: 0;
      font-size: 24px;
      color: #333;
    }

    .filters {
      margin-bottom: 20px;
    }

    .filter-select {
      padding: 8px 12px;
      border: 1px solid #ddd;
      border-radius: 4px;
      background: white;
      cursor: pointer;
    }

    .table-container {
      background: white;
      border-radius: 8px;
      overflow: hidden;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .coupons-table {
      width: 100%;
      border-collapse: collapse;
    }

    .coupons-table thead {
      background: #f9f9f9;
      border-bottom: 2px solid #ddd;
    }

    .coupons-table th {
      padding: 12px;
      text-align: left;
      font-weight: 600;
      color: #333;
    }

    .coupons-table td {
      padding: 12px;
      border-bottom: 1px solid #eee;
    }

    .coupons-table tr:hover {
      background: #f9f9f9;
    }

    .coupons-table tr.inactive {
      opacity: 0.6;
    }

    .badge {
      display: inline-block;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      font-weight: 500;
    }

    .badge-platform {
      background: #e3f2fd;
      color: #1976d2;
    }

    .badge-seller {
      background: #f3e5f5;
      color: #7b1fa2;
    }

    .badge-intelligent {
      background: #e0f2f1;
      color: #00796b;
    }

    .badge-planbased {
      background: #fff3e0;
      color: #e65100;
    }

    .badge-success {
      background: #c8e6c9;
      color: #2e7d32;
    }

    .badge-danger {
      background: #ffcdd2;
      color: #c62828;
    }

    .actions {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .btn-sm {
      padding: 6px 10px;
      font-size: 12px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      transition: all 0.3s;
    }

    .btn-primary {
      background: #1976d2;
      color: white;
      padding: 10px 20px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
    }

    .btn-primary:hover {
      background: #1565c0;
    }

    .btn-info {
      background: #0288d1;
      color: white;
    }

    .btn-info:hover {
      background: #01579b;
    }

    .btn-warning {
      background: #f57c00;
      color: white;
    }

    .btn-warning:hover {
      background: #e65100;
    }

    .btn-danger {
      background: #d32f2f;
      color: white;
    }

    .btn-danger:hover {
      background: #b71c1c;
    }

    .btn-secondary {
      background: #757575;
      color: white;
      padding: 10px 20px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }

    .btn-close {
      background: none;
      border: none;
      font-size: 28px;
      cursor: pointer;
      color: #333;
    }

    .modal {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0,0,0,0.5);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 1000;
    }

    .modal-content {
      background: white;
      border-radius: 8px;
      width: 90%;
      max-width: 600px;
      max-height: 90vh;
      overflow-y: auto;
    }

    .modal-details {
      max-width: 700px;
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 20px;
      border-bottom: 1px solid #ddd;
    }

    .modal-header h3 {
      margin: 0;
      color: #333;
    }

    .modal-body {
      padding: 20px;
    }

    .form-group {
      margin-bottom: 16px;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
    }

    .form-group label {
      display: block;
      margin-bottom: 6px;
      font-weight: 500;
      color: #333;
    }

    .form-control {
      width: 100%;
      padding: 8px 12px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
    }

    .form-control:focus {
      outline: none;
      border-color: #1976d2;
      box-shadow: 0 0 0 2px rgba(25, 118, 210, 0.1);
    }

    .form-checkboxes {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .checkbox {
      display: flex;
      align-items: center;
      cursor: pointer;
    }

    .checkbox input {
      margin-right: 8px;
      cursor: pointer;
    }

    .modal-footer {
      padding: 20px;
      border-top: 1px solid #ddd;
      display: flex;
      justify-content: flex-end;
      gap: 10px;
    }

    .empty-state {
      text-align: center;
      padding: 40px 20px;
      color: #666;
    }

    .details-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;
      margin-bottom: 30px;
    }

    .detail-item label {
      display: block;
      font-weight: 600;
      color: #666;
      font-size: 12px;
      text-transform: uppercase;
      margin-bottom: 6px;
    }

    .detail-item p {
      margin: 0;
      color: #333;
      font-size: 16px;
    }

    .usage-history {
      border-top: 1px solid #ddd;
      padding-top: 20px;
    }

    .usage-history h4 {
      margin: 0 0 16px 0;
      color: #333;
    }

    .usage-item {
      display: flex;
      justify-content: space-between;
      padding: 10px 0;
      border-bottom: 1px solid #eee;
      font-size: 14px;
    }

    .usage-item .amount {
      color: #d32f2f;
      font-weight: 600;
    }

    .usage-item .date {
      color: #999;
    }
  `]
})
export class CouponManagementComponent implements OnInit, OnDestroy {
  coupons: Coupon[] = [];
  couponForm!: FormGroup;
  showModal = false;
  showDetailsModal = false;
  loading = false;
  editingCoupon: Coupon | null = null;
  selectedCoupon: Coupon | null = null;
  couponUsage: any = null;
  selectedType = '';

  private destroy$ = new Subject<void>();

  constructor(
    private couponService: CouponService,
    private fb: FormBuilder
  ) {
    this.initializeForm();
  }

  ngOnInit() {
    this.loadCoupons();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  initializeForm() {
    this.couponForm = this.fb.group({
      code: ['', [Validators.required, Validators.minLength(3)]],
      description: [''],
      type: [1, Validators.required],  // 1 = Platform
      discountType: [1, Validators.required],  // 1 = Percentage
      discountValue: [0, [Validators.required, Validators.min(0.01)]],
      maxDiscount: [null],
      minOrderValue: [0],
      scope: [1, Validators.required],  // 1 = EntireOrder
      validFrom: ['', Validators.required],
      validUntil: ['', Validators.required],
      usageLimit: [0],
      usageLimitPerUser: [1],
      isActive: [true],
      preventsCombination: [true],
      onlyWithoutPromotion: [false],
      onlyFirstPurchase: [false]
    });
  }

  loadCoupons() {
    this.loading = true;
    this.couponService.getAllCoupons(this.selectedType || undefined)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (coupons) => {
          this.coupons = coupons;
          this.loading = false;
        },
        error: (err) => {
          console.error('Erro ao carregar cupons:', err);
          this.loading = false;
        }
      });
  }

  openCreateModal() {
    this.editingCoupon = null;
    const today = new Date();
    const nextMonth = new Date(today.getFullYear(), today.getMonth() + 1, today.getDate());
    
    // Converter para formato YYYY-MM-DD para input date
    const todayStr = today.toISOString().split('T')[0];
    const nextMonthStr = nextMonth.toISOString().split('T')[0];
    
    this.couponForm.reset({
      type: 1,
      discountType: 1,
      validFrom: todayStr,
      validUntil: nextMonthStr,
      isActive: true,
      preventsCombination: true,
      usageLimitPerUser: 1
    });
    this.showModal = true;
  }

  editCoupon(coupon: Coupon) {
    this.editingCoupon = coupon;
    this.couponForm.patchValue({
      code: coupon.code,
      description: coupon.description,
      type: coupon.type,
      discountType: coupon.discountType,
      discountValue: coupon.discountValue,
      maxDiscount: coupon.maxDiscount,
      minOrderValue: coupon.minOrderValue,
      validFrom: coupon.validFrom,
      validUntil: coupon.validUntil,
      usageLimit: coupon.usageLimit,
      usageLimitPerUser: coupon.usageLimitPerUser,
      isActive: coupon.isActive,
      preventsCombination: coupon.preventsCombination,
      onlyWithoutPromotion: coupon.onlyWithoutPromotion,
      onlyFirstPurchase: coupon.onlyFirstPurchase
    });
    this.showModal = true;
  }

  saveCoupon() {
    if (!this.couponForm.valid) return;

    this.loading = true;
    const formValue = this.couponForm.value;

    if (this.editingCoupon) {
      this.couponService.updateCoupon(this.editingCoupon.id, formValue)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.loadCoupons();
            this.closeModal();
          },
          error: (err) => {
            console.error('Erro ao atualizar cupom:', err);
            this.loading = false;
          }
        });
    } else {
      this.couponService.createCoupon(formValue)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.loadCoupons();
            this.closeModal();
          },
          error: (err) => {
            console.error('Erro ao criar cupom:', err);
            this.loading = false;
          }
        });
    }
  }

  deleteCoupon(id: string) {
    if (confirm('Tem certeza que deseja deletar este cupom?')) {
      this.loading = true;
      this.couponService.deleteCoupon(id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.loadCoupons();
          },
          error: (err) => {
            console.error('Erro ao deletar cupom:', err);
            this.loading = false;
          }
        });
    }
  }

  viewDetails(coupon: Coupon) {
    this.selectedCoupon = coupon;
    this.showDetailsModal = true;
    this.couponService.getCouponUsage(coupon.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (usage) => {
          this.couponUsage = usage;
        }
      });
  }

  closeModal() {
    this.showModal = false;
    this.editingCoupon = null;
  }

  closeDetailsModal() {
    this.showDetailsModal = false;
    this.selectedCoupon = null;
  }

  getCouponTypeName(type: number): string {
    const typeMap: { [key: number]: string } = {
      1: 'Plataforma',
      2: 'Seller',
      3: 'Inteligente',
      4: 'Por Plano'
    };
    return typeMap[type] || 'Desconhecido';
  }

  getCouponTypeClass(type: number): string {
    const classMap: { [key: number]: string } = {
      1: 'platform',
      2: 'seller',
      3: 'intelligent',
      4: 'planbased'
    };
    return classMap[type] || 'platform';
  }
}
