import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ProductService } from '../../services/product/product.service';
import { AuthService } from '../../services/auth/auth.service';
import { ShippingService } from '../../services/shipping/shipping.service';
import { OrderService } from '../../services/order/order.service';
import { SellerService } from '../../services/seller/seller.service';

import { Product } from '../../models/product/product.model';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';

interface DailyRevenue {
  date: string;
  revenue: number;
}

interface DashboardStats {
  sellerId: string;
  totalRevenue: number;
  totalSales: number;
  activeProducts: number;
  dailyRevenue: DailyRevenue[];
}

@Component({
  selector: 'app-seller-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    CurrencyBrPipe,
    LoadingSpinnerComponent
  ],
  templateUrl: './seller-dashboard.html',
  styleUrl: './seller-dashboard.css'
})
export class SellerDashboardComponent implements OnInit {

  private productService = inject(ProductService);
  private authService = inject(AuthService);
  private shippingService = inject(ShippingService);
  private orderService = inject(OrderService);
  private sellerService = inject(SellerService);

  activeTab: 'overview' | 'products' | 'orders' = 'overview';

  products: Product[] = [];
  recentOrders: any[] = [];

  isLoading = true;
  isDeleting = false;
  isEditing = false;
  isSaving = false;

  editingProduct: any = {};
  errorMessage: string | null = null;

  stats: DashboardStats = {
    sellerId: '',
    totalRevenue: 0,
    totalSales: 0,
    activeProducts: 0,
    dailyRevenue: []
  };

  stripeStatus: { isConnected: boolean; accountId?: string; chargesEnabled?: boolean; detailsSubmitted?: boolean } | null = null;
  isStripeLoading = false;

  chartDays: string[] = [];
  ngOnInit(): void {
    this.loadDashboardData();
  }

  setActiveTab(tab: 'overview' | 'products' | 'orders') {
    this.activeTab = tab;
  }

  // ==========================
  // DASHBOARD
  // ==========================
  loadDashboardData() {
    this.isLoading = true;
    this.errorMessage = null;

    const currentUser = this.authService.currentUserValue;

    if (!currentUser || currentUser.role !== 'Seller') {
      this.errorMessage = 'Acesso negado.';
      this.isLoading = false;
      return;
    }

    this.sellerService.getDashboardData().subscribe({
      next: (data: DashboardStats) => {
        this.loadStripeStatus();
        this.stats = data;
        this.mapChartDays();

        if (data.sellerId) {
          this.loadSellerContent(data.sellerId);
        } else {
          this.isLoading = false;
        }
      },
      error: () => {
        this.errorMessage = 'Erro ao carregar dashboard.';
        this.isLoading = false;
      }
    });
  }

  private loadSellerContent(sellerId: string) {
    forkJoin({
      products: this.productService.getAllProducts(1, 100, '', undefined, undefined, undefined, undefined, sellerId)
        .pipe(catchError(() => of([]))),
      orders: this.orderService.getMyOrders().pipe(catchError(() => of([])))
    }).subscribe({
      next: ({ products, orders }) => {
        const allProducts = Array.isArray(products) ? products : (products?.data || products?.items || []);
        this.products = allProducts.filter((p: any) => p.sellerId === sellerId);

        this.recentOrders = this.processOrders(orders, sellerId);
        this.isLoading = false;
      }
    });
  }

  // ==========================
  // ORDERS
  // ==========================
  private processOrders(allOrders: any[], sellerId: string): any[] {
    if (!allOrders?.length) return [];

    const hasSellerInfo = allOrders.some(o =>
      o.items?.some((i: any) => i?.product?.sellerId || i?.sellerId)
    );

    const filtered = hasSellerInfo
      ? allOrders.filter(o =>
          o.items?.some((i: any) =>
            i.product?.sellerId === sellerId || i.sellerId === sellerId
          )
        )
      : allOrders;

    return filtered.map(o => ({
      id: o.id,
      displayId: '#' + o.id.slice(0, 8).toUpperCase(),
      customer: o.customerName || 'Cliente',
      date: new Date(o.createdAt).toLocaleDateString('pt-BR'),
      total: o.totalAmount,
      status: this.translateStatus(o.status),
      trackingCode: o.trackingCode,
      carrier: o.carrier,
      items: o.items,
      primaryItemName: o.items?.[0]?.productName || 'Produto',
      primaryItemImage: o.items?.[0]?.productImage || '',
      itemsCount: o.items?.length || 0
    }));
  }

  private translateStatus(status: any): string {
    const map: any = {
      0: 'Pendente',
      1: 'Confirmado',
      2: 'Processando',
      3: 'Enviado',
      4: 'Entregue',
      5: 'Cancelado',
      6: 'Reembolsado',
      'Paid': 'Confirmado',
      'Confirmed': 'Confirmado',
      'Processing': 'Processando',
      'Sent': 'Enviado',
      'Delivered': 'Entregue',
      'Refunded': 'Reembolsado'
    };
    return map[status] || 'Desconhecido';
  }
  private loadStripeStatus() {
    this.isStripeLoading = true;
    this.sellerService.getStripeStatus().subscribe({
      next: (status) => {
        this.stripeStatus = status;
        this.isStripeLoading = false;
      },
      error: () => {
        this.isStripeLoading = false;
      }
    });
  }

  connectStripe() {
    if (this.isStripeLoading) return;
    this.isStripeLoading = true;
    this.sellerService.createStripeConnectLink().subscribe({
      next: (res) => {
        window.location.href = res.url;
      },
      error: (err) => {
        this.isStripeLoading = false;
        const msg = err?.error?.message || 'Erro ao iniciar conexao com Stripe.';
        alert(msg);
      }
    });
  }

  manageStripe() {
    if (this.isStripeLoading) return;
    this.isStripeLoading = true;
    this.sellerService.createStripeDashboardLink().subscribe({
      next: (res) => {
        window.location.href = res.url;
      },
      error: (err) => {
        this.isStripeLoading = false;
        const msg = err?.error?.message || 'Erro ao abrir portal do Stripe.';
        alert(msg);
      }
    });
  }

  // ==========================
  // PRODUCTS
  // ==========================
  onDelete(product: Product) {
    if (!confirm(`Excluir "${product.name}"?`)) return;

    this.isDeleting = true;
    this.productService.deleteProduct(product.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.loadDashboardData(); // 🔥 CORRETO
      },
      error: () => {
        this.isDeleting = false;
        alert('Erro ao excluir produto.');
      }
    });
  }

  openEditModal(product: Product) {
    const discountPercent = product.salePrice && product.price
      ? Math.max(0, Math.round((1 - product.salePrice / product.price) * 100))
      : 0;
    this.editingProduct = { ...product, discountPercent };
    this.isEditing = true;
  }

  closeEditModal() {
    this.isEditing = false;
    this.editingProduct = {};
  }

  saveProduct() {
    this.isSaving = true;

    const discountPercent = this.parseNumber(this.editingProduct.discountPercent);
    const price = this.parseNumber(this.editingProduct.price);
    const normalizedDiscount = Math.min(Math.max(discountPercent, 0), 90);
    const salePrice = normalizedDiscount > 0 && price > 0
      ? Number((price * (1 - normalizedDiscount / 100)).toFixed(2))
      : null;

    const updateData = {
      id: this.editingProduct.id,
      name: this.editingProduct.name,
      description: this.editingProduct.description,
      price: price > 0 ? price : null,
      stockQuantity: this.editingProduct.stockQuantity,
      salePrice,
      maxInstallments: this.editingProduct.maxInstallments,
      maxNoInterestInstallments: this.editingProduct.maxNoInterestInstallments
    };

    console.log('[seller-dashboard] updateProduct payload', updateData);

    this.productService.updateProduct(this.editingProduct.id, updateData)
      .subscribe({
        next: () => {
          this.isSaving = false;
          this.closeEditModal();
          this.loadDashboardData();
        },
        error: () => {
          this.isSaving = false;
          alert('Erro ao salvar produto.');
        }
      });
  }

  getDiscountPercent(product: Product): number {
    if (!product.salePrice || !product.price) return 0;
    return Math.max(0, Math.round((1 - product.salePrice / product.price) * 100));
  }

  getSalePricePreview(): number | null {
    const price = this.parseNumber(this.editingProduct.price);
    const discountPercent = this.parseNumber(this.editingProduct.discountPercent);
    const normalizedDiscount = Math.min(Math.max(discountPercent, 0), 90);
    if (price <= 0 || normalizedDiscount <= 0) return null;
    return Number((price * (1 - normalizedDiscount / 100)).toFixed(2));
  }

  private parseNumber(value: unknown): number {
    if (typeof value === 'number') return Number.isFinite(value) ? value : 0;
    if (typeof value !== 'string') return 0;
    const trimmed = value.trim();
    if (!trimmed) return 0;
    const normalized = trimmed.includes(',')
      ? trimmed.replace(/\./g, '').replace(',', '.')
      : trimmed;
    const parsed = Number(normalized);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  // ==========================
  // SHIPPING
  // ==========================
  generateLabel(order: any) {
    this.isLoading = true;

    this.shippingService.generateLabel(order.id).subscribe({
      next: res => {
        window.open(res.labelUrl, '_blank');
        if (res.warning) {
          alert(res.warning);
        }
        this.loadDashboardData();
      },
      error: (err) => {
        this.isLoading = false;
        const msg = err?.error?.message || 'Erro ao gerar etiqueta. Verifique o endereço/CEP e a configuração do Melhor Envio.';
        alert(msg);
      }
    });
  }
  addTrackingManual(order: any) {
    const code = prompt('Codigo de rastreio:');
    if (!code) return;

    const carrier = prompt('Transportadora:', 'Correios');
    if (!carrier) return;

    this.isLoading = true;
    this.orderService.updateTracking(order.id, code, carrier).subscribe({
      next: () => {
        order.trackingCode = code;
        order.carrier = carrier;
        order.status = 'Enviado';
        this.loadDashboardData();
      },
      error: (err) => {
        this.isLoading = false;
        const msg = err?.error?.message || 'Erro ao atualizar rastreio.';
        alert(msg);
      }
    });
  }

  // ==========================
  // CHART
  // ==========================
  private mapChartDays() {
    this.chartDays = this.stats.dailyRevenue.map(d =>
      new Date(d.date).toLocaleDateString('pt-BR', { weekday: 'short' })
    );
  }

  get maxRevenue(): number {
    return Math.max(...this.stats.dailyRevenue.map(d => d.revenue), 1);
  }

  getBarHeight(index: number): number {
    return (this.stats.dailyRevenue[index].revenue / this.maxRevenue) * 90;
  }

  getRevenueForDay(index: number): number {
    return this.stats.dailyRevenue[index].revenue;
  }
}
