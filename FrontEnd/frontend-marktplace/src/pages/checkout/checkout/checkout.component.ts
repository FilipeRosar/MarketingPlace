import { Component, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CartService } from '../../../services/cart/cart.service';
import { CheckoutService } from '../../../services/checkout/checkout.service';
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
  private checkoutService = inject(CheckoutService);
  private authService = inject(AuthService);
  private router = inject(Router);

  isLoading = false;
  cartItems = this.cartService.cartItems;

  subtotal = this.cartService.total;

  // 2. Taxa de Serviço (Deve bater com a constante SERVICE_FEE_CENTS = 299 do Backend)
  serviceFee = 2.99;

  total = computed(() => this.subtotal() + this.serviceFee);

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

    const itemsToBuy = this.cartItems();

    this.checkoutService.createCheckoutSession(itemsToBuy).subscribe({
      next: (response: any) => {
        if (response.url) {
          window.location.href = response.url;
        } else {
          console.error('URL não encontrada');
          this.isLoading = false;
          alert('Erro ao processar.');
        }
      },
      error: (err) => {
        console.error('Erro no checkout:', err);
        this.isLoading = false;
        const msg = err.error?.message || 'Erro ao iniciar pagamento.';
        alert(msg);
      }
    });
  }
}
