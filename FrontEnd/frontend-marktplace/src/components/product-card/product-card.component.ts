import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Product } from '../../models/product/product.model';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';
import { CartService } from '../../services/cart/cart.service';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, CurrencyBrPipe, RouterLink],
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.css'
})
export class ProductCardComponent {
  @Input({ required: true }) product!: Product;

  private cartService = inject(CartService);

  addToCart(event: Event, product: Product) {
    event.stopPropagation();

    this.cartService.addToCart(product);
    console.log('Adicionado ao carrinho:', product.name);
  }
}
