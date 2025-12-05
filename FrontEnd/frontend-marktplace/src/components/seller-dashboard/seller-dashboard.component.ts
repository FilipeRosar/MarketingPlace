import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ProductService } from '../../services/product/product.service';
import { AuthService } from '../../services/auth/auth.service';
import { ShippingService } from '../../services/shipping/shipping.service';
import { OrderService } from '../../services/order/order.service';
import { Product } from '../../models/product/product.model';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-seller-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyBrPipe, LoadingSpinnerComponent],
  templateUrl: './seller-dashboard.html',
  styleUrl: './seller-dashboard.css'
})
export class SellerDashboardComponent implements OnInit {
  private productService = inject(ProductService);
  private authService = inject(AuthService);
  private shippingService = inject(ShippingService);
  private orderService = inject(OrderService);
  private destroyRef = inject(DestroyRef);

  activeTab: 'overview' | 'products' | 'orders' = 'overview';

  products: Product[] = [];
  recentOrders: any[] = [];
  isLoading = true;
  isDeleting = false;

  // Pega o usuário atual, mas não bloqueia se for nulo (para testes de layout)
  currentUser = this.authService.currentUserValue;
  errorMessage: string | null = null;

  stats = {
    totalRevenue: 0,
    totalSales: 0,
    productsActive: 0
  };

  ngOnInit() {
    this.loadDataIndependently();
  }

  setActiveTab(tab: 'overview' | 'products' | 'orders') {
    this.activeTab = tab;
  }

  // Nova estratégia: Carrega dados separadamente para que um erro não quebre o outro
  loadDataIndependently() {
    this.isLoading = true;
    this.errorMessage = null;

    // 1. Carregar Produtos
    this.productService.getAllProducts()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(err => {
          console.warn('Falha ao carregar produtos (Dashboard):', err);
          return of([]); // Retorna lista vazia se der erro, não quebra o fluxo
        })
      )
      .subscribe((data: any) => {
        const allProducts = Array.isArray(data) ? data : (data?.items || data?.data || []);

        if (this.currentUser) {
           // Filtra produtos deste vendedor
           this.products = allProducts.filter((p: any) => p.sellerId === this.currentUser?.id);
        } else {
           // Se não tiver user (teste), mostra tudo ou nada (ajuste conforme preferir)
           // this.products = allProducts;
        }

        this.updateStats();
        this.isLoading = false; // Libera a tela assim que produtos carregarem
      });

    // 2. Carregar Vendas (Pedidos) em paralelo
    // Se esta rota falhar (404), não vai afetar a exibição dos produtos acima
    this.orderService.getMyOrders()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(err => {
          console.warn('Falha ao carregar pedidos (Dashboard):', err);
          // Retorna array vazio para a tela não travar
          return of([]);
        })
      )
      .subscribe((orders: any[]) => {
        // Processa apenas se houver pedidos e usuário logado
        if (this.currentUser && orders.length > 0) {
             this.recentOrders = this.processOrders(orders);
             this.updateStats();
        }
      });
  }

  private processOrders(allOrders: any[]): any[] {
    if (!allOrders) return [];

    // Mapeia para o formato da tabela
    return allOrders.map(o => ({
      id: o.id,
      displayId: '#' + (o.id ? o.id.slice(0, 8).toUpperCase() : '???'),
      customer: 'Cliente Trama',
      date: o.createdAt ? new Date(o.createdAt).toLocaleDateString('pt-BR') : 'Data desc.',
      total: o.totalAmount || 0,
      status: this.translateStatus(o.status),
      trackingCode: o.trackingCode,
      carrier: o.carrier
    }));
  }

  private updateStats() {
    this.stats.productsActive = this.products.length;
    this.stats.totalSales = this.recentOrders.length;
    this.stats.totalRevenue = this.recentOrders.reduce((acc, curr) => acc + (curr.total || 0), 0);
  }

  private translateStatus(status: any): string {
    const map: any = { 0: 'Pendente', 1: 'Pago', 2: 'Enviado', 3: 'Entregue', 4: 'Cancelado' };
    return map[status] || status || 'Desconhecido';
  }

  // --- AÇÕES ---

  onDelete(product: Product) {
    if(confirm(`Excluir "${product.name}"?`)) {
        this.isDeleting = true;
        this.productService.deleteProduct(product.id).subscribe({
            next: () => {
                this.products = this.products.filter(p => p.id !== product.id);
                this.updateStats();
                this.isDeleting = false;
                alert('Produto excluído.');
            },
            error: () => {
                this.isDeleting = false;
                alert('Erro ao excluir.');
            }
        });
    }
  }

  generateLabel(order: any) {
    this.isLoading = true;
    this.shippingService.generateLabel(order.id).subscribe({
        next: (res) => {
            window.open(res.labelUrl, '_blank');
            this.isLoading = false;
            order.status = 'Enviado';
            alert('Etiqueta gerada!');
        },
        error: () => {
            this.isLoading = false;
            alert('Erro ao gerar etiqueta.');
        }
    });
  }

  addTrackingManual(order: any) {
    const code = prompt("Código de rastreio:");
    if (!code) return;
    const carrier = prompt("Transportadora:", "Correios");
    if (!carrier) return;

    order.trackingCode = code;
    order.carrier = carrier;
    order.status = 'Enviado';
  }
}
