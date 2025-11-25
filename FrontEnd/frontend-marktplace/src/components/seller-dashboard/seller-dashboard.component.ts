import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ProductService } from '../../services/product/product.service';
import { AuthService } from '../../services/auth/auth.service';
import { Product } from '../../models/product/product.model';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';

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
  private destroyRef = inject(DestroyRef);

  activeTab: 'overview' | 'products' | 'orders' = 'overview';

  products: Product[] = [];
  isLoading = true;
  currentUser = this.authService.currentUserValue;
  errorMessage: string | null = null;

  stats = { totalRevenue: 1259.90, totalSales: 14, views: 342 };
  recentOrders = [
    { id: '#TR-8823', customer: 'Maria Silva', date: '24/11/2025', total: 159.90, status: 'Pago' },
    { id: '#TR-8821', customer: 'João Pedro', date: '23/11/2025', total: 89.90, status: 'Enviado' },
    { id: '#TR-8819', customer: 'Ana Clara', date: '22/11/2025', total: 245.00, status: 'Entregue' }
  ];

  ngOnInit() {
    this.loadMyProducts();
  }

  setActiveTab(tab: 'overview' | 'products' | 'orders') {
    this.activeTab = tab;
  }

  loadMyProducts() {
    this.isLoading = true;
    this.errorMessage = null;

    if (!this.currentUser || this.currentUser.role !== 'Seller') {
      this.errorMessage = 'Acesso negado. Você não tem permissão de vendedor.';
      this.isLoading = false;
      return;
    }

    this.productService.getAllProducts()
      .pipe(
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data: any) => {
          try {
            const allProducts = Array.isArray(data) ? data : (data?.items || data?.data || []);

            // Filtra produtos pelo ID do vendedor logado
            this.products = allProducts.filter((p: any) =>
              p.sellerId === this.currentUser?.id // Filtra pelo ID do vendedor
            );

            if (this.products.length === 0) {
              this.errorMessage = 'Você ainda não cadastrou produtos.';
            }
          } catch (error) {
            console.error('Erro ao processar produtos:', error);
            this.errorMessage = 'Ocorreu um erro ao carregar os dados.';
          }
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Erro na requisição da API (Dashboard):', err);
          if (err.status === 401 || err.status === 403) {
            this.errorMessage = 'Sessão expirada. Faça login novamente.';
          } else {
            this.errorMessage = 'Erro ao conectar com o servidor.';
          }
          this.isLoading = false;
        }
      });
  }
}
