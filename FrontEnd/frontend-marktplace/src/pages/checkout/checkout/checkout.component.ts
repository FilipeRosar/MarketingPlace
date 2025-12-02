import { CheckoutService } from './../../../services/checkout/checkout.service';
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CartService } from '../../../services/cart/cart.service';
import { OrderService } from '../../../services/order/order.service';
import { CurrencyBrPipe } from '../../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../../components/loading-spinner.component/loading-spinner.component';
import { AuthService } from '../../../services/auth/auth.service';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, CurrencyBrPipe, LoadingSpinnerComponent],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css'
})
export class CheckoutComponent implements OnInit {
  cartService = inject(CartService);
  private orderService = inject(OrderService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private checkoutService = inject(CheckoutService);
  isLoading = false;
  cartItems = this.cartService.cartItems;
  total = this.cartService.total;

  ngOnInit() {
    if (!this.authService.currentUserValue) {
      this.router.navigate(['/login']);
      return;
    }

    if (this.cartItems().length === 0) {
      this.router.navigate(['/']);
    }
  }

  onCheckout() {
  if (this.cartItems().length === 0) {
    alert('Carrinho vazio!');
    return;
  }

  this.isLoading = true;

  this.checkoutService.createCheckoutSession(this.cartItems()).subscribe({
    next: (res) => {
      window.location.href = res.url;
    },
    error: (err) => {
      this.isLoading = false;
      alert(err.error?.message || 'Erro ao iniciar pagamento');
    }
  });


  this.isLoading = true;

  this.checkoutService.createCheckoutSession(this.cartItems()).subscribe({
    next: (res) => {
      window.location.href = res.url; // Redireciona para o Stripe
    },
    error: (err) => {
      this.isLoading = false;
      const mensagem = err.error?.message || 'Erro ao iniciar pagamento. Tente novamente.';
      alert(mensagem);
    }
  });

}
}
