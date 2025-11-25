import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { ProductService } from '../../services/product/product.service';
import { Product } from '../../models/product/product.model';
import { ProductCardComponent } from '../../components/product-card/product-card.component';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, ProductCardComponent, LoadingSpinnerComponent, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  private productService = inject(ProductService);
  private route = inject(ActivatedRoute);

  products: Product[] = [];
  isLoading = true;

  pageTitle = 'Destaques da Semana';

  currentPage = 1;
  pageSize = 12;
  totalPages = 1;
  totalItems = 0;

  currentSearch = '';
  currentCategory = '';

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.currentSearch = params['search'] || '';
      this.currentCategory = params['category'] || '';

      this.currentPage = 1;

      if (this.currentSearch) {
        this.pageTitle = `Resultados para "${this.currentSearch}"`;
      } else if (this.currentCategory) {
        this.pageTitle = 'Filtrado por Categoria';
      } else {
        this.pageTitle = 'Destaques da Semana';
      }

      this.loadProducts();
    });
  }

  loadProducts() {
    this.isLoading = true;

    this.productService.getAllProducts(this.currentPage, this.pageSize, this.currentSearch, this.currentCategory).subscribe({
      next: (data: any) => {
        let items = [];

        if (Array.isArray(data)) {
          items = data;
          this.totalItems = items.length;
        } else if (data?.items && Array.isArray(data.items)) {
          items = data.items;
          this.totalItems = data.total || 0;
        } else if (data?.data && Array.isArray(data.data)) {
          items = data.data;
          this.totalItems = data.total || data.meta?.total || 0;
        } else {
          items = [];
          this.totalItems = 0;
        }

        this.products = items;

        this.totalPages = Math.ceil(this.totalItems / this.pageSize);
        if (this.totalPages < 1) this.totalPages = 1;

        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erro ao carregar produtos:', err);
        this.isLoading = false;
      }
    });
  }

  changePage(newPage: number) {
    if (newPage >= 1 && newPage <= this.totalPages && newPage !== this.currentPage) {
      this.currentPage = newPage;
      this.loadProducts();

      const productSection = document.getElementById('produtos');
      if (productSection) {
        productSection.scrollIntoView({ behavior: 'smooth' });
      }
    }
  }
}
