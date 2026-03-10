import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';

export interface CouponFormData {
  code: string;
  description: string;
  discountType: 'percentage' | 'fixed';
  discountValue: number;
  scope: 'product' | 'category' | 'store';
  scopeId?: string;
  validFrom: string;
  validUntil: string;
  usageLimit: number;
  isActive: boolean;
}

@Component({
  selector: 'app-seller-coupon-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="form-container">
      <h3>{{ mode === 'create' ? '✨ Criar Novo Cupom' : '✏️ Editar Cupom' }}</h3>

      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <!-- Basic Info Section -->
        <fieldset class="form-section">
          <legend>Informações Básicas</legend>
          
          <div class="form-grid">
            <div class="form-group">
              <label>Código do Cupom *</label>
              <input 
                type="text" 
                formControlName="code"
                placeholder="Ex: SUMMER2024"
                maxlength="50"
                class="input"
              />
              <span class="error" *ngIf="isFieldInvalid('code')">
                Código obrigatório (3-50 caracteres)
              </span>
            </div>

            <div class="form-group">
              <label>Descrição *</label>
              <input 
                type="text" 
                formControlName="description"
                placeholder="Ex: Promoção de Verão"
                maxlength="100"
                class="input"
              />
              <span class="error" *ngIf="isFieldInvalid('description')">
                Descrição obrigatória (3-100 caracteres)
              </span>
            </div>
          </div>
        </fieldset>

        <!-- Discount Section -->
        <fieldset class="form-section">
          <legend>Desconto</legend>
          
          <div class="form-grid">
            <div class="form-group">
              <label>Tipo de Desconto *</label>
              <select formControlName="discountType" class="input">
                <option [value]="1">Percentual (%)</option>
                <option [value]="2">Valor Fixo (R$)</option>
              </select>
            </div>

            <div class="form-group">
              <label>Valor do Desconto *</label>
              <input 
                type="number" 
                formControlName="discountValue"
                [placeholder]="form.get('discountType')?.value === 1 ? 'Ex: 10' : 'Ex: 25,00'"
                step="0.01"
                min="0"
                class="input"
              />
              <span class="error" *ngIf="isFieldInvalid('discountValue')">
                Valor de desconto obrigatório (> 0)
              </span>
            </div>

            <div class="form-group">
              <label>Escopo *</label>
              <select formControlName="scope" class="input">
                <option [value]="1">Toda a Loja</option>
                <option [value]="2">Produto Específico</option>
                <option [value]="3">Categoria</option>
              </select>
            </div>

            <div class="form-group" *ngIf="form.get('scope')?.value !== 1">
              <label>{{ form.get('scope')?.value === 2 ? 'ID do Produto' : 'ID da Categoria' }} *</label>
              <input 
                type="text" 
                formControlName="scopeId"
                placeholder="Ex: 123"
                class="input"
              />
              <span class="error" *ngIf="isFieldInvalid('scopeId')">
                ID obrigatório quando escopo não é toda a loja
              </span>
            </div>
          </div>
        </fieldset>

        <!-- Validity Section -->
        <fieldset class="form-section">
          <legend>Validade e Limite de Uso</legend>
          
          <div class="form-grid">
            <div class="form-group">
              <label>Válido de *</label>
              <input 
                type="date" 
                formControlName="validFrom"
                class="input"
              />
              <span class="error" *ngIf="isFieldInvalid('validFrom')">
                Data de início obrigatória
              </span>
            </div>

            <div class="form-group">
              <label>Válido até *</label>
              <input 
                type="date" 
                formControlName="validUntil"
                class="input"
              />
              <span class="error" *ngIf="isFieldInvalid('validUntil')">
                Data de término obrigatória
              </span>
            </div>

            <div class="form-group">
              <label>Limite de Usos *</label>
              <input 
                type="number" 
                formControlName="usageLimit"
                placeholder="Ex: 100"
                min="1"
                class="input"
              />
              <span class="error" *ngIf="isFieldInvalid('usageLimit')">
                Limite de usos obrigatório (> 0)
              </span>
            </div>

            <div class="form-group checkbox-group">
              <label>
                <input 
                  type="checkbox" 
                  formControlName="isActive"
                />
                Ativo
              </label>
            </div>
          </div>
        </fieldset>

        <!-- Form Actions -->
        <div class="form-actions">
          <button 
            type="submit" 
            class="btn-submit"
            [disabled]="!form.valid"
          >
            {{ mode === 'create' ? 'Criar Cupom' : 'Salvar Alterações' }}
          </button>
          <button 
            type="button" 
            class="btn-cancel"
            (click)="onCancel()"
          >
            Cancelar
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .form-container {
      background: white;
      padding: 25px;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      max-width: 700px;
    }

    h3 {
      margin: 0 0 20px 0;
      font-size: 18px;
      color: #333;
    }

    .form-section {
      border: none;
      padding: 20px 0;
      border-bottom: 1px solid #e0e0e0;
      margin: 0;
    }

    .form-section:last-of-type {
      border-bottom: none;
    }

    legend {
      font-size: 14px;
      font-weight: 600;
      color: #555;
      margin-bottom: 15px;
      padding: 0;
    }

    .form-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 20px;
    }

    .form-group {
      display: flex;
      flex-direction: column;
    }

    label {
      font-size: 13px;
      font-weight: 500;
      color: #333;
      margin-bottom: 6px;
    }

    .input,
    select {
      padding: 10px 12px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
      font-family: inherit;
      transition: border-color 0.2s;
    }

    .input:focus,
    select:focus {
      outline: none;
      border-color: #0066cc;
      box-shadow: 0 0 0 2px rgba(0, 102, 204, 0.1);
    }

    .checkbox-group {
      justify-content: flex-end;
    }

    .checkbox-group label {
      display: flex;
      align-items: center;
      gap: 8px;
      margin: 0;
    }

    .checkbox-group input[type="checkbox"] {
      width: 18px;
      height: 18px;
      cursor: pointer;
    }

    .error {
      font-size: 12px;
      color: #dc3545;
      margin-top: 4px;
    }

    .form-actions {
      display: flex;
      gap: 10px;
      justify-content: flex-end;
      margin-top: 25px;
      padding-top: 20px;
      border-top: 1px solid #e0e0e0;
    }

    .btn-submit,
    .btn-cancel {
      padding: 10px 24px;
      border: none;
      border-radius: 4px;
      font-weight: 600;
      font-size: 14px;
      cursor: pointer;
      transition: background 0.2s;
    }

    .btn-submit {
      background: #0066cc;
      color: white;
    }

    .btn-submit:hover:not(:disabled) {
      background: #0052a3;
    }

    .btn-submit:disabled {
      background: #ccc;
      cursor: not-allowed;
    }

    .btn-cancel {
      background: #f0f0f0;
      color: #333;
    }

    .btn-cancel:hover {
      background: #e0e0e0;
    }
  `]
})
export class SellerCouponFormComponent implements OnInit {
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() initialData?: CouponFormData;
  @Output() submitted = new EventEmitter<CouponFormData>();
  @Output() cancelled = new EventEmitter<void>();

  form!: FormGroup;

  constructor(private fb: FormBuilder) {}

  ngOnInit() {
    this.initializeForm();
  }

  private initializeForm() {
    const today = new Date().toISOString().split('T')[0];
    const nextMonth = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];

    this.form = this.fb.group({
      code: [this.initialData?.code || '', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
      description: [this.initialData?.description || '', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
      type: [2, Validators.required],  // 2 = Seller
      discountType: [1, Validators.required],  // 1 = Percentage
      discountValue: [this.initialData?.discountValue || '', [Validators.required, Validators.min(0.01)]],
      maxDiscount: ['', Validators.min(0)],
      minOrderValue: [0, Validators.min(0)],
      scope: [1, Validators.required],  // 1 = EntireOrder
      scopeId: [''],  // Product/Category ID
      sellerId: ['seller-123'],
      validFrom: [this.initialData?.validFrom || today, Validators.required],
      validUntil: [this.initialData?.validUntil || nextMonth, Validators.required],
      usageLimit: [this.initialData?.usageLimit || 100, [Validators.required, Validators.min(1)]],
      usageLimitPerUser: [5, [Validators.required, Validators.min(1)]],
      isActive: [this.initialData?.isActive !== false],
      preventsCombination: [true],
      onlyWithoutPromotion: [false],
      onlyFirstPurchase: [false]
    });

    // Adicionar validação condicional para scopeId
    this.form.get('scope')?.valueChanges.subscribe((scope) => {
      const scopeIdControl = this.form.get('scopeId');
      if (scope === 1) {  // EntireOrder
        scopeIdControl?.clearAsyncValidators();
        scopeIdControl?.clearValidators();
      } else {
        scopeIdControl?.setValidators([Validators.required]);
      }
      scopeIdControl?.updateValueAndValidity();
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSubmit() {
    if (this.form.valid) {
      this.submitted.emit(this.form.value);
    }
  }

  onCancel() {
    this.cancelled.emit();
  }
}
