import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ProductService } from '../../services/product/product.service';
import { AuthService } from '../../services/auth/auth.service';
import { ShippingService } from '../../services/shipping/shipping.service';
import { OrderService } from '../../services/order/order.service';
import { Product } from '../../models/product/product.model';
import { Order } from '../../models/order/order.model';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-seller-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyBrPipe, LoadingSpinnerComponent, FormsModule],
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

  // Edição
  isEditing = false;
  isSaving = false;
  editingProduct: any = {};

  currentUser = this.authService.currentUserValue;
  errorMessage: string | null = null;

  stats = {
    totalRevenue: 0,
    totalSales: 0,
    productsActive: 0
  };

  ngOnInit() {
    this.loadDashboardData();
  }

  setActiveTab(tab: any) {
    this.activeTab = tab;
  }

  loadDashboardData() {
    this.isLoading = true;
    this.errorMessage = null;

    if (!this.currentUser || this.currentUser.role !== 'Seller') {
      this.errorMessage = 'Acesso negado. Você não tem permissão de vendedor.';
      this.isLoading = false;
      return;
    }

    // Carrega dados em paralelo, tratando erros individualmente para não quebrar a tela toda
    forkJoin({
      products: this.productService.getAllProducts().pipe(catchError(() => of([]))),
      orders: this.orderService.getMyOrders().pipe(catchError(() => of([])))
    })
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: ({ products, orders }) => {
        try {
          // 1. Processar Produtos
          const allProducts = Array.isArray(products) ? products : (products?.items || products?.data || []);
          // Filtra apenas produtos deste vendedor
          this.products = allProducts.filter((p: any) => p.sellerId === this.currentUser?.id);

          // 2. Processar Pedidos (Vendas)
          // Nota: O endpoint atual retorna COMPRAS do usuário.
          // Para um dashboard real, precisaríamos de um endpoint que retorna VENDAS onde o usuário é o Vendedor.
          // Aqui tentamos filtrar, mas se o objeto Order não tiver itens detalhados com SellerId, pode vir vazio.
          this.recentOrders = this.processOrders(orders, this.currentUser!.id);

          // 3. Calcular Estatísticas
          this.calculateStats();

        } catch (error) {
          console.error('Erro ao processar dados:', error);
          this.errorMessage = 'Erro ao processar dados do painel.';
        } finally {
          this.isLoading = false;
        }
      },
      error: (err) => {
        console.error('Erro crítico no Dashboard:', err);
        this.isLoading = false;
        if (err.status === 401) this.errorMessage = 'Sessão expirada.';
        else this.errorMessage = 'Falha de conexão com o servidor.';
      }
    });
  }

  private processOrders(allOrders: any[], sellerId: string): any[] {
    if (!allOrders || allOrders.length === 0) return [];

    // Tenta filtrar pedidos que contenham produtos deste vendedor
    // Se a API de Orders não retornar detalhes do Seller dentro dos Items, isso pode não funcionar 100%
    const sellerOrders = allOrders.filter(o =>
       o.items && o.items.some((i: any) => i.sellerId === sellerId || true) // '|| true' para DEBUG (remove em prod se tiver filtro real)
    );

    return sellerOrders.map(o => ({
      id: o.id,
      displayId: '#' + (o.id ? o.id.slice(0, 8).toUpperCase() : '???'),
      customer: 'Cliente Trama', // Placeholder (Backend precisa enviar nome do comprador)
      date: o.createdAt ? new Date(o.createdAt).toLocaleDateString('pt-BR') : 'Data desc.',
      total: o.totalAmount || 0,
      status: this.translateStatus(o.status),
      trackingCode: o.trackingCode,
      carrier: o.carrier,
      items: o.items
    }));
  }

  private calculateStats() {
    this.stats.productsActive = this.products.length;
    this.stats.totalSales = this.recentOrders.length;
    this.stats.totalRevenue = this.recentOrders.reduce((acc, curr) => acc + (curr.total || 0), 0);
  }

  private translateStatus(status: any): string {
    // Mapeia enum ou string do backend para texto amigável
    const map: any = {
        0: 'Pendente', 'Pending': 'Pendente',
        1: 'Pago', 'Paid': 'Pago',
        2: 'Enviado', 'Shipped': 'Enviado', 'Sent': 'Enviado',
        3: 'Entregue', 'Delivered': 'Entregue',
        4: 'Cancelado', 'Canceled': 'Cancelado'
    };
    return map[status] || status || 'Desconhecido';
  }

  // --- AÇÕES DE PRODUTO ---

  onDelete(product: Product) {
    if(confirm(`Tem certeza que deseja excluir "${product.name}"?`)) {
        this.isDeleting = true;
        this.productService.deleteProduct(product.id).subscribe({
            next: () => {
                this.products = this.products.filter(p => p.id !== product.id);
                this.calculateStats();
                this.isDeleting = false;
                alert('Produto excluído com sucesso.');
            },
            error: (err) => {
                this.isDeleting = false;
                console.error(err);
                alert('Erro ao excluir produto.');
            }
        });
    }
  }

  // --- EDIÇÃO DE PRODUTO (MODAL) ---

  openEditModal(product: Product) {
    this.editingProduct = { ...product }; // Clona para editar
    this.isEditing = true;
  }

  closeEditModal() {
    this.isEditing = false;
    this.editingProduct = {};
  }

  saveProduct() {
    this.isSaving = true;

    const updateDto = {
        id: this.editingProduct.id,
        name: this.editingProduct.name,
        description: this.editingProduct.description,
        price: this.editingProduct.price,
        salePrice: this.editingProduct.salePrice || null,
        stockQuantity: this.editingProduct.stockQuantity,
        category: this.editingProduct.category,
        tags: this.editingProduct.tags || []
    };

    this.productService.updateProduct(this.editingProduct.id, updateDto).subscribe({
        next: () => {
            alert('Produto atualizado!');
            this.isSaving = false;
            this.closeEditModal();
            this.loadDashboardData(); // Recarrega a lista
        },
        error: (err) => {
            console.error(err);
            alert('Erro ao atualizar produto.');
            this.isSaving = false;
        }
    });
  }

  // --- GESTÃO DE ENVIO ---

  generateLabel(order: any) {
    this.isLoading = true;
    this.shippingService.generateLabel(order.id).subscribe({
        next: (res) => {
            window.open(res.labelUrl, '_blank');
            this.isLoading = false;

            // Atualiza visualmente
            const foundOrder = this.recentOrders.find(o => o.id === order.id);
            if (foundOrder) {
                foundOrder.status = 'Enviado';
                foundOrder.trackingCode = 'Gerado Automático';
            }
            alert('Etiqueta gerada com sucesso!');
        },
        error: (err) => {
            this.isLoading = false;
            console.error(err);
            alert('Erro ao gerar etiqueta. Verifique o endereço do cliente.');
        }
    });
  }

  addTrackingManual(order: any) {
    const code = prompt("Digite o código de rastreio (ex: AA123456789BR):");
    if (!code) return;

    const carrier = prompt("Qual a transportadora?", "Correios");
    if (!carrier) return;

    // Atualiza visualmente (Idealmente chamaria API para salvar)
    const foundOrder = this.recentOrders.find(o => o.id === order.id);
    if (foundOrder) {
        foundOrder.trackingCode = code;
        foundOrder.carrier = carrier;
        foundOrder.status = 'Enviado';
    }
  }
}
