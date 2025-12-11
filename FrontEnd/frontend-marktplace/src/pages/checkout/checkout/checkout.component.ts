import { Component, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CartService } from '../../../services/cart/cart.service';
import { CheckoutService } from '../../../services/checkout/checkout.service';
import { CurrencyBrPipe } from '../../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../../components/loading-spinner.component/loading-spinner.component';
import { AuthService } from '../../../services/auth/auth.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, CurrencyBrPipe, LoadingSpinnerComponent],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css'
})
export class CheckoutComponent implements OnInit {
  cartService = inject(CartService);
  private checkoutService = inject(CheckoutService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private notificationService = inject(NotificationService); // Injetar

  isLoading = false;
  cartItems = this.cartService.cartItems;

  subtotal = this.cartService.total;
  serviceFee = 0;

  total = computed(() => this.subtotal() + this.serviceFee);
  private http = inject(HttpClient);
  ngOnInit() {
    this.http.get<number>(`${environment.apiUrl}/settings/service-fee`).subscribe({
    next: (fee) => this.serviceFee = fee,
    error: () => this.serviceFee = 2.99
    });
    if (!this.authService.currentUserValue) {
      this.notificationService.info('Faça login para finalizar a compra.', 'Atenção');
      this.router.navigate(['/login']);
      return;
    }

    if (this.cartItems().length === 0) {
      this.router.navigate(['/']);
    }
  }

  onCheckout() {
    if (this.cartItems().length === 0) {
      this.notificationService.warning('Seu carrinho está vazio!', 'Ops!');
      return;
    }

    this.isLoading = true;

    const itemsToBuy = this.cartItems();

    this.checkoutService.createCheckoutSession(itemsToBuy).subscribe({
      next: (response: any) => {
        if (response.url) {
          // SUCESSO: Limpa o carrinho local antes de ir para o Stripe
          this.cartService.clearCart();

          // Redireciona
          window.location.href = response.url;
        } else {
          console.error('URL de pagamento não encontrada');
          this.isLoading = false;
          this.notificationService.error('Erro ao processar pagamento. Tente novamente.', 'Erro');
        }
      },
      error: (err) => {
        console.error('Erro no checkout:', err);
        this.isLoading = false;
        const msg = err.error?.message || 'Erro ao iniciar pagamento. Tente novamente.';
        this.notificationService.error(msg, 'Erro no Checkout');
      }
    });
  }
}
