import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
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

  products: Product[] = [];
  isLoading = true;

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.productService.getAllProducts().subscribe({
      next: (data: any) => {
        console.log('Resposta da API de Produtos:', data);

        if (Array.isArray(data)) {
          this.products = data;
        } else if (data?.items && Array.isArray(data.items)) {
          this.products = data.items;
        } else if (data?.data && Array.isArray(data.data)) {
          this.products = data.data;
        } else {
          console.warn('Formato de dados inesperado. Esperado array ou objeto com items/data.');
          this.products = [];
        }

        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erro crítico ao carregar produtos:', err);
        this.isLoading = false;
      }
    });
  }
}
