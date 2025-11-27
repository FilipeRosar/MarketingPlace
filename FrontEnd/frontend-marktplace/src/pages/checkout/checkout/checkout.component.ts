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
    this.isLoading = true;

    this.orderService.createCheckoutSession([]).subscribe({
      next: (response: any) => {
        if (response.url) {
          window.location.href = response.url;
        } else {
          console.error('URL de pagamento não encontrada na resposta');
          this.isLoading = false;
          alert('Erro ao processar pagamento.');
        }
      },
      error: (err) => {
        console.error('Erro no checkout:', err);
        this.isLoading = false;
        const msg = err.error?.message || 'Erro ao iniciar pagamento. Tente novamente.';
        alert(msg);
      }
    });
  }
}
