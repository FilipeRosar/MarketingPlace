import { Component, OnInit, OnDestroy, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CartService } from '../../../services/cart/cart.service';
import { CheckoutService } from '../../../services/checkout/checkout.service';
import { CurrencyBrPipe } from '../../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../../components/loading-spinner.component/loading-spinner.component';
import { AuthService } from '../../../services/auth/auth.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { ShippingService } from '../../../services/shipping/shipping.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Subject, interval, startWith, switchMap, takeUntil } from 'rxjs';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyBrPipe, LoadingSpinnerComponent],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css'
})
export class CheckoutComponent implements OnInit, OnDestroy {
  cartService = inject(CartService);
  private checkoutService = inject(CheckoutService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private notificationService = inject(NotificationService); // Injetar
  private shippingService = inject(ShippingService);
  private destroy$ = new Subject<void>();

  isLoading = false;
  cartItems = this.cartService.cartItems;

  subtotal = this.cartService.total;
  serviceFee = signal(0);
  shippingOptions = signal<any[]>([]);
  selectedShipping = signal<any | null>(null);
  shippingError = '';
  shippingNotice = '';
  isCalculatingShipping = false;
  zipCodeFrom = '';
  zipCodeTo = '';

  shippingFee = computed(() => this.selectedShipping()?.price ?? 0);
  total = computed(() => this.subtotal() + this.serviceFee() + this.shippingFee());
  private http = inject(HttpClient);
  ngOnInit() {
    interval(30000)
      .pipe(
        startWith(0),
        switchMap(() => this.http.get<{ fee: number }>(`${environment.apiUrl}/settings/service-fee`)),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (response) => this.serviceFee.set(response.fee),
        error: () => this.serviceFee.set(2.99)
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

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  calculateShipping() {
    this.shippingError = '';
    this.shippingNotice = '';

    const zipFrom = this.normalizeZip(this.zipCodeFrom);
    const zipTo = this.normalizeZip(this.zipCodeTo);

    if (zipFrom.length !== 8 || zipTo.length !== 8) {
      this.shippingError = 'Informe CEPs validos (8 digitos).';
      return;
    }

    const { items, usedFallback } = this.buildShippingItems();
    if (items.length === 0) {
      this.shippingError = 'Nao foi possivel montar os itens para calculo de frete.';
      return;
    }

    if (usedFallback) {
      this.shippingNotice = 'Alguns produtos estao sem dimensoes. Usando valores padrao para o frete.';
    }

    this.isCalculatingShipping = true;
    this.shippingService.calculateShipping(zipFrom, zipTo, items).subscribe({
      next: (options: any[]) => {
        const customOptions = [
          { name: 'Retirada no local', price: 0, deliveryTime: 0, companyLogo: '' },
          { name: 'Entrega combinada com o vendedor', price: 0, deliveryTime: 0, companyLogo: '' }
        ];
        const normalized = (options || []).map(o => ({
          name: o.name ?? 'Frete',
          price: Number(o.price) || 0,
          deliveryTime: o.deliveryTime ?? 0,
          companyLogo: o.companyLogo ?? ''
        }));
        const all = [...customOptions, ...normalized];
        this.shippingOptions.set(all);
        this.selectedShipping.set(all[0] ?? null);
        this.isCalculatingShipping = false;
      },
      error: () => {
        this.shippingError = 'Erro ao calcular frete. Tente novamente.';
        this.shippingOptions.set([]);
        this.selectedShipping.set(null);
        this.isCalculatingShipping = false;
      }
    });
  }

  selectShipping(option: any) {
    this.selectedShipping.set(option);
  }

  private normalizeZip(value: string): string {
    return (value || '').replace(/\D/g, '');
  }

  private buildShippingItems(): { items: any[]; usedFallback: boolean } {
    let usedFallback = false;
    const items = this.cartItems().map(item => {
      const weight = item.product.weight && item.product.weight > 0 ? item.product.weight : 0.3;
      const width = item.product.width && item.product.width > 0 ? item.product.width : 11;
      const height = item.product.height && item.product.height > 0 ? item.product.height : 2;
      const length = item.product.length && item.product.length > 0 ? item.product.length : 16;
      if (!item.product.weight || !item.product.width || !item.product.height || !item.product.length) {
        usedFallback = true;
      }
      return {
        weight,
        width,
        height,
        length,
        quantity: item.quantity
      };
    });

    return { items, usedFallback };
  }

  onCheckout() {
    if (this.cartItems().length === 0) {
      this.notificationService.warning('Seu carrinho está vazio!', 'Ops!');
      return;
    }

    this.isLoading = true;

    const itemsToBuy = this.cartItems();
    const selected = this.selectedShipping();
    const shippingFee = selected?.price ?? 0;
    const shippingName = selected?.name ?? 'Frete';

    this.checkoutService.createCheckoutSession(itemsToBuy, shippingFee, shippingName).subscribe({
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
