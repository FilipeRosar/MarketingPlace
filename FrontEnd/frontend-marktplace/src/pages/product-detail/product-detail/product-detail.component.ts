import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProductService } from '../../../services/product/product.service';
import { CartService } from '../../../services/cart/cart.service';
import { Product } from '../../../models/product/product.model';
import { CurrencyBrPipe } from '../../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../../components/loading-spinner.component/loading-spinner.component';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyBrPipe, LoadingSpinnerComponent],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.css'
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private location = inject(Location);
  product: Product | null = null;
  isLoading = true;
  error: string | null = null;

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.loadProduct(id);
      }
    });
  }

  loadProduct(id: string) {
    this.isLoading = true;
    this.productService.getProductById(id).subscribe({
      next: (data) => {
        this.product = data;
        this.isLoading = false;
        window.scrollTo(0, 0);
      },
      error: (err) => {
        console.error('Erro ao carregar produto:', err);
        this.error = 'Produto não encontrado ou indisponível.';
        this.isLoading = false;
      }
    });
  }

  addToCart() {
    if (this.product) {
      this.cartService.addToCart(this.product);
      alert('Produto adicionado ao carrinho! (Implementar Toast aqui)');
    }
  }

  goBack() {
    this.location.back();
  }
}
