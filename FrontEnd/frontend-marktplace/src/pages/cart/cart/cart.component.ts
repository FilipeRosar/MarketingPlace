import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartService } from '../../../services/cart/cart.service';
import { CurrencyBrPipe } from '../../../shared/pipes/currency-br-pipe';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyBrPipe],
  templateUrl: './cart.html',
  styles: [`
    input[type="number"]::-webkit-inner-spin-button,
    input[type="number"]::-webkit-outer-spin-button {
      -webkit-appearance: none;
      margin: 0;
    }
  `]
})
export class CartComponent {
  cartService = inject(CartService);

  cartItems = this.cartService.cartItems;
  total = this.cartService.total;

  updateQuantity(productId: string, quantity: number) {
    if (quantity > 0) {
      this.cartService.updateQuantity(productId, quantity);
    }
  }

  removeItem(productId: string) {
    this.cartService.removeFromCart(productId);
  }
}
