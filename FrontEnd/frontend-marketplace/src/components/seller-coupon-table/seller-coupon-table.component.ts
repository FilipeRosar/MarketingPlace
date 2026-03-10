import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Coupon } from '../../services/coupon/coupon.service';

@Component({
  selector: 'app-seller-coupon-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="coupon-table-container">
      <!-- Filters -->
      <div class="filters-row">
        <select [(ngModel)]="statusFilter" (change)="applyFilters()" class="filter-select">
          <option value="">Todos os Status</option>
          <option value="active">Ativos</option>
          <option value="inactive">Inativos</option>
        </select>
        <select [(ngModel)]="typeFilter" (change)="applyFilters()" class="filter-select">
          <option value="">Todos os Tipos</option>
          <option value="Seller">Seller</option>
          <option value="Platform">Platform</option>
          <option value="Intelligent">Inteligente</option>
        </select>
        <input 
          type="text" 
          [(ngModel)]="searchTerm" 
          (keyup)="applyFilters()"
          placeholder="Buscar por código..." 
          class="search-input"
        />
      </div>

      <!-- Table -->
      <table class="coupons-table">
        <thead>
          <tr>
            <th>Código</th>
            <th>Desconto</th>
            <th>Tipo</th>
            <th>Uso</th>
            <th>Validade</th>
            <th>Status</th>
            <th>Ações</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let coupon of filteredCoupons">
            <td class="code-cell"><strong>{{ coupon.code }}</strong></td>
            <td>
              <span *ngIf="coupon.discountType === 1">{{ coupon.discountValue }}%</span>
              <span *ngIf="coupon.discountType === 2">R$ {{ coupon.discountValue | number: '1.2-2' }}</span>
            </td>
            <td>{{ coupon.type }}</td>
            <td>{{ coupon.usageCount }}/{{ coupon.usageLimit || '∞' }}</td>
            <td>{{ coupon.validUntil | date: 'dd/MM/yyyy' }}</td>
            <td>
              <span [class.badge-active]="coupon.isActive" [class.badge-inactive]="!coupon.isActive" class="badge">
                {{ coupon.isActive ? 'Ativo' : 'Inativo' }}
              </span>
            </td>
            <td class="actions-cell">
              <button (click)="edit.emit(coupon)" class="btn-action btn-edit" title="Editar">
                ✏️
              </button>
              <button (click)="viewAnalytics.emit(coupon.id)" class="btn-action btn-chart" title="Analytics">
                📊
              </button>
              <button (click)="clone.emit(coupon)" class="btn-action btn-clone" title="Clonar">
                🔁
              </button>
              <button (click)="delete.emit(coupon.id)" class="btn-action btn-delete" title="Deletar">
                🗑️
              </button>
            </td>
          </tr>
          <tr *ngIf="filteredCoupons.length === 0">
            <td colspan="7" class="empty-state">
              <p>Nenhum cupom encontrado</p>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    .coupon-table-container {
      padding: 20px;
      background: #f8f9fa;
      border-radius: 8px;
    }

    .filters-row {
      display: flex;
      gap: 10px;
      margin-bottom: 20px;
      flex-wrap: wrap;
    }

    .filter-select,
    .search-input {
      padding: 8px 12px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
      flex: 1;
      min-width: 150px;
    }

    .search-input {
      min-width: 200px;
    }

    .coupons-table {
      width: 100%;
      border-collapse: collapse;
      background: white;
      border-radius: 8px;
      overflow: hidden;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
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
    }

    tbody tr:hover {
      background: #fafafa;
    }

    .code-cell {
      font-family: monospace;
      color: #0066cc;
    }

    .badge {
      padding: 4px 12px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 500;
    }

    .badge-active {
      background: #d4edda;
      color: #155724;
    }

    .badge-inactive {
      background: #f8d7da;
      color: #721c24;
    }

    .actions-cell {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .btn-action {
      background: none;
      border: none;
      font-size: 18px;
      cursor: pointer;
      padding: 4px 8px;
      transition: transform 0.2s;
    }

    .btn-action:hover {
      transform: scale(1.2);
    }

    .empty-state {
      text-align: center;
      padding: 40px 20px;
      color: #999;
    }
  `]
})
export class SellerCouponTableComponent implements OnInit, OnChanges {
  @Input() coupons: Coupon[] = [];
  @Output() edit = new EventEmitter<Coupon>();
  @Output() delete = new EventEmitter<string>();
  @Output() viewAnalytics = new EventEmitter<string>();
  @Output() clone = new EventEmitter<Coupon>();

  filteredCoupons: Coupon[] = [];
  statusFilter = '';
  typeFilter = '';
  searchTerm = '';

  ngOnInit() {
    this.applyFilters();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['coupons']) {
      this.applyFilters();
    }
  }

  applyFilters() {
    this.filteredCoupons = this.coupons.filter(coupon => {
      const matchStatus = !this.statusFilter || 
        (this.statusFilter === 'active' ? coupon.isActive : !coupon.isActive);
      
      let matchType = true;
      if (this.typeFilter) {
        const typeMap: { [key: string]: number } = {
          'Platform': 1,
          'Seller': 2,
          'Intelligent': 3,
          'PlanBased': 4
        };
        const filterValue = typeMap[this.typeFilter];
        matchType = coupon.type === filterValue;
      }
      
      const matchSearch = !this.searchTerm || 
        coupon.code.toLowerCase().includes(this.searchTerm.toLowerCase());

      return matchStatus && matchType && matchSearch;
    });
  }
}
