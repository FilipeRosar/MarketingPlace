import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ProductService } from '../../services/product/product.service';
import { AuthService } from '../../services/auth/auth.service';
import { ShippingService } from '../../services/shipping/shipping.service';
import { OrderService } from '../../services/order/order.service';
import { SellerPlan, SellerService, SellerSubscription } from '../../services/seller/seller.service';
import { PromotionService, Promotion } from '../../services/promotion/promotion.service';

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
  previousRevenue?: number;
  totalSales: number;
  previousSales?: number;
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
  private promotionService = inject(PromotionService);
  private route = inject(ActivatedRoute);

  activeTab: 'overview' | 'products' | 'promotions' | 'orders' | 'subscription' = 'overview';

  products: Product[] = [];
  recentOrders: any[] = [];
  promotions: Promotion[] = [];

  isLoading = true;
  isDeleting = false;
  isEditing = false;
  isSaving = false;

  // Promoções
  isPromotionModalOpen = false;
  isSavingPromotion = false;
  editingPromotion: Partial<Promotion> = {};
  filteredProducts: Product[] = [];
  productSearchTerm = '';

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

  subscription: SellerSubscription | null = null;
  isSubscriptionLoading = false;
  isSubscribing = false;
  subscriptionMessage: string | null = null;

  sellerPlans: Array<{
    key: SellerPlan;
    name: string;
    price: number;
    highlights: string[];
  }> = [
    {
      key: 'Basic',
      name: 'Basic',
      price: 0,
      highlights: ['Sem mensalidade', 'Comissao 12%', 'Sem destaque']
    },
    {
      key: 'Pro',
      name: 'Pro',
      price: 29.99,
      highlights: ['Comissao 9%', 'Destaques 8', 'Analytics avancado']
    },
    {
      key: 'Premium',
      name: 'Premium',
      price: 59.9,
      highlights: ['Comissao 5%', 'Destaques 15', 'Suporte prioritario']
    }
  ];

  chartDays: string[] = [];

  ngOnInit(): void {
    this.loadDashboardData();
    this.route.queryParamMap.subscribe(params => {
      if (params.get('subscription')) {
        this.subscriptionMessage = params.get('subscription') === 'success'
          ? 'Assinatura confirmada. Aguarde a atualizacao do plano.'
          : 'Assinatura cancelada.';
        this.loadSubscription();
      }
    });
  }

  setActiveTab(tab: 'overview' | 'products' | 'promotions' | 'orders' | 'subscription') {
    this.activeTab = tab;

    // Carrega promoções quando abrir a aba
    if (tab === 'promotions' && this.promotions.length === 0) {
      this.loadPromotions();
    }
  }

  // ==========================
  // PROMOÇÕES
  // ==========================

  loadPromotions() {
    this.promotionService.getMyPromotions().subscribe({
      next: (promos) => {
        // Enriquece com nomes dos produtos
        this.promotions = promos.map(promo => ({
          ...promo,
          productNames: promo.productIds
            .map(id => this.products.find(p => p.id === id)?.name)
            .filter(Boolean) as string[],
          productCount: promo.productIds.length
        }));
      },
      error: (err) => {
        console.error('Erro ao carregar promoções:', err);
      }
    });
  }

  openPromotionModal(promo?: Promotion) {
    if (promo) {
      // Edição - converte datas para formato do input
      this.editingPromotion = {
        ...promo,
        startDate: this.toDatetimeLocal(promo.startDate),
        endDate: this.toDatetimeLocal(promo.endDate)
      };
    } else {
      // Nova promoção
      const now = new Date();
      const tomorrow = new Date(now.getTime() + 24 * 60 * 60 * 1000);

      this.editingPromotion = {
        name: '',
        description: '',
        discountPercentage: 10,
        productIds: [],
        startDate: this.toDatetimeLocal(now),
        endDate: this.toDatetimeLocal(tomorrow),
        isActive: true
      };
    }

    this.filteredProducts = [...this.products];
    this.productSearchTerm = '';
    this.isPromotionModalOpen = true;
  }

  closePromotionModal() {
    this.isPromotionModalOpen = false;
    this.editingPromotion = {};
    this.filteredProducts = [];
    this.productSearchTerm = '';
  }

  filterProducts() {
    const term = this.productSearchTerm.toLowerCase().trim();

    if (!term) {
      this.filteredProducts = [...this.products];
      return;
    }

    this.filteredProducts = this.products.filter(p =>
      p.name.toLowerCase().includes(term) ||
      p.description?.toLowerCase().includes(term)
    );
  }

  isProductSelected(productId: string): boolean {
    return this.editingPromotion.productIds?.includes(productId) || false;
  }

  toggleProductSelection(productId: string) {
    if (!this.editingPromotion.productIds) {
      this.editingPromotion.productIds = [];
    }

    const index = this.editingPromotion.productIds.indexOf(productId);

    if (index > -1) {
      this.editingPromotion.productIds.splice(index, 1);
    } else {
      this.editingPromotion.productIds.push(productId);
    }
  }

  isPromotionValid(): boolean {
    return !!(
      this.editingPromotion.name?.trim() &&
      this.editingPromotion.discountPercentage &&
      this.editingPromotion.discountPercentage > 0 &&
      this.editingPromotion.discountPercentage <= 90 &&
      this.editingPromotion.startDate &&
      this.editingPromotion.endDate &&
      this.editingPromotion.productIds &&
      this.editingPromotion.productIds.length > 0
    );
  }

  savePromotion() {
    if (!this.isPromotionValid() || this.isSavingPromotion) return;

    this.isSavingPromotion = true;

    // Converte datas de datetime-local para ISO
    const promotionData: Partial<Promotion> = {
      ...this.editingPromotion,
      startDate: new Date(this.editingPromotion.startDate!).toISOString(),
      endDate: new Date(this.editingPromotion.endDate!).toISOString()
    };

    const operation = this.editingPromotion.id
      ? this.promotionService.updatePromotion(this.editingPromotion.id, promotionData)
      : this.promotionService.createPromotion(promotionData);

    operation.subscribe({
      next: () => {
        this.isSavingPromotion = false;
        this.closePromotionModal();
        this.loadPromotions();
      },
      error: (err) => {
        this.isSavingPromotion = false;
        const msg = err?.error?.message || 'Erro ao salvar promoção.';
        alert(msg);
      }
    });
  }

  editPromotion(promo: Promotion) {
    this.openPromotionModal(promo);
  }

  togglePromotionStatus(promo: Promotion) {
    const newStatus = !promo.isActive;
    const action = newStatus ? 'ativar' : 'desativar';

    if (!confirm(`Deseja ${action} esta promoção?`)) return;

    this.promotionService.updatePromotion(promo.id, { isActive: newStatus }).subscribe({
      next: () => {
        promo.isActive = newStatus;
      },
      error: (err) => {
        const msg = err?.error?.message || `Erro ao ${action} promoção.`;
        alert(msg);
      }
    });
  }

  deletePromotion(promo: Promotion) {
    if (!confirm(`Excluir a promoção "${promo.name}"? Esta ação não pode ser desfeita.`)) return;

    this.promotionService.deletePromotion(promo.id).subscribe({
      next: () => {
        this.loadPromotions();
      },
      error: (err) => {
        const msg = err?.error?.message || 'Erro ao excluir promoção.';
        alert(msg);
      }
    });
  }

  isPromotionActive(promo: Promotion): boolean {
    if (!promo.isActive) return false;

    const now = new Date();
    const start = new Date(promo.startDate);
    const end = new Date(promo.endDate);

    return now >= start && now <= end;
  }

  isPromotionUpcoming(promo: Promotion): boolean {
    if (!promo.isActive) return false;

    const now = new Date();
    const start = new Date(promo.startDate);

    return now < start;
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  private toDatetimeLocal(date: string | Date): string {
    const d = new Date(date);
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');

    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  // ==========================
  // DASHBOARD (código existente)
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
        this.loadSubscription();
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

  calculateGrowth(current: number, previous: number): number {
    if (!previous || previous === 0) return current > 0 ? 100 : 0;
    return Math.round(((current - previous) / previous) * 100);
  }

  getRevenueGrowth(): number {
    return this.calculateGrowth(
      this.stats.totalRevenue,
      this.stats.previousRevenue || 0);
  }

  getSalesGrowth(): number {
    return this.calculateGrowth(
      this.stats.totalSales,
      this.stats.previousSales || 0);
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
  // ORDERS (código existente)
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

  private loadSubscription() {
    this.isSubscriptionLoading = true;
    this.sellerService.getSubscription().subscribe({
      next: (subscription) => {
        this.subscription = subscription;
        this.isSubscriptionLoading = false;
      },
      error: (err) => {
        if (err?.status === 404) {
          this.subscription = null;
        }
        this.isSubscriptionLoading = false;
      }
    });
  }

  startSubscription(plan: SellerPlan) {
    if (this.isSubscribing) return;
    this.subscriptionMessage = null;
    this.isSubscribing = true;
    this.sellerService.createSubscriptionCheckout(plan).subscribe({
      next: (res) => {
        if (res.url) {
          window.location.href = res.url;
          return;
        }
        if (res.subscription) {
          this.subscription = res.subscription;
        }
        this.isSubscribing = false;
      },
      error: (err) => {
        this.isSubscribing = false;
        const msg = err?.error?.message || 'Erro ao iniciar assinatura.';
        alert(msg);
      }
    });
  }

  cancelSubscription() {
    if (this.isSubscribing) return;
    this.isSubscribing = true;
    this.sellerService.cancelSubscription().subscribe({
      next: () => {
        this.isSubscribing = false;
        this.loadSubscription();
      },
      error: (err) => {
        this.isSubscribing = false;
        const msg = err?.error?.message || 'Erro ao cancelar assinatura.';
        alert(msg);
      }
    });
  }

  isCurrentPlan(plan: SellerPlan): boolean {
    return !!(this.subscription?.isActive && this.subscription.plan === plan);
  }

  getSubscriptionStatusLabel(): string {
    if (!this.subscription) return 'Sem assinatura ativa';
    return this.subscription.isActive ? `Plano ${this.subscription.plan}` : 'Assinatura inativa';
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
  // PRODUCTS (código existente)
  // ==========================
  onDelete(product: Product) {
    if (!confirm(`Excluir "${product.name}"?`)) return;

    this.isDeleting = true;
    this.productService.deleteProduct(product.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.loadDashboardData();
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
  // SHIPPING (código existente)
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
