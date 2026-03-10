import { Component, OnInit, OnDestroy, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CartService } from '../../../services/cart/cart.service';
import { CheckoutService } from '../../../services/checkout/checkout.service';
import { CouponService } from '../../../services/coupon/coupon.service';
import { CurrencyBrPipe } from '../../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../../components/loading-spinner.component/loading-spinner.component';
import { AuthService } from '../../../services/auth/auth.service';
import { NotificationService } from '../../../services/notification/notification.service';
import { ShippingService } from '../../../services/shipping/shipping.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Subject, interval, startWith, switchMap, takeUntil } from 'rxjs';

export interface ShippingSelection {
  sellerId: string;
  sellerName: string;
  selectedOption: any | null;
}

interface ItemBySeller {
  sellerId: string;
  sellerName: string;
  items: any[];
  shippingOptions: any[];
  selectedShipping: any | null;
  isCalculating: boolean;
}

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
  private notificationService = inject(NotificationService);
  private shippingService = inject(ShippingService);
  private destroy$ = new Subject<void>();

  isLoading = false;
  cartItems = this.cartService.cartItems;

  subtotal = this.cartService.total;
  serviceFee = signal(0);
  couponCode = '';
  couponDiscount = signal(0);
  couponValidation = signal<any>(null);
  validatingCoupon = signal(false);
  zipCodeTo = '';

  // Itens agrupados por vendedor
  itemsBySeller = signal<ItemBySeller[]>([]);

  // Calcula frete total (soma de todos os vendedores)
  totalShippingFee = computed(() => {
    return this.itemsBySeller().reduce((acc, seller) => {
      return acc + (seller.selectedShipping?.price ?? 0);
    }, 0);
  });

  total = computed(() => this.subtotal() + this.serviceFee() + this.totalShippingFee() - this.couponDiscount());

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

    this.groupItemsBySeller();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private groupItemsBySeller(): void {
    const grouped = new Map<string, ItemBySeller>();

    for (const item of this.cartItems()) {
      const sellerId = item.product.sellerId || 'unknown';
      const sellerName = item.product.sellerName || 'Vendedor Desconhecido';

      if (!grouped.has(sellerId)) {
        grouped.set(sellerId, {
          sellerId,
          sellerName,
          items: [],
          shippingOptions: [],
          selectedShipping: null,
          isCalculating: false
        });
      }

      grouped.get(sellerId)!.items.push(item);
    }

    this.itemsBySeller.set(Array.from(grouped.values()));
  }

  calculateShipping(): void {
    this.zipCodeTo = this.normalizeZip(this.zipCodeTo);

    if (this.zipCodeTo.length !== 8) {
      this.notificationService.error('Informe um CEP valido (8 dígitos).', 'Erro');
      return;
    }

    const sellers = this.itemsBySeller();
    if (sellers.length === 0) {
      this.notificationService.error('Nenhum item no carrinho.', 'Erro');
      return;
    }

    // Marcar todos como calculando
    sellers.forEach(s => s.isCalculating = true);
    this.itemsBySeller.set([...sellers]);

    const itemsBySeller = sellers.map(seller => ({
      sellerId: seller.sellerId,
      items: this.buildShippingItems(seller.items)
    }));

    this.shippingService.calculateShippingBySeller(itemsBySeller, this.zipCodeTo).subscribe({
      next: (result: Record<string, any[]>) => {
        const updated = sellers.map(seller => {
          const options = result[seller.sellerId] || [];
          return {
            ...seller,
            shippingOptions: options,
            selectedShipping: options.length > 0 ? options[0] : null,
            isCalculating: false
          };
        });
        this.itemsBySeller.set(updated);
      },
      error: () => {
        this.notificationService.error('Erro ao calcular frete. Tente novamente.', 'Erro');
        sellers.forEach(s => s.isCalculating = false);
        this.itemsBySeller.set([...sellers]);
      }
    });
  }

  selectShipping(sellerId: string, option: any): void {
    const updated = this.itemsBySeller().map(seller =>
      seller.sellerId === sellerId
        ? { ...seller, selectedShipping: option }
        : seller
    );
    this.itemsBySeller.set(updated);
  }

  private normalizeZip(value: string): string {
    return (value || '').replace(/\D/g, '');
  }

  private buildShippingItems(items: any[]): any[] {
    return items.map(item => ({
      weight: item.product.weight && item.product.weight > 0 ? item.product.weight : 0.3,
      width: item.product.width && item.product.width > 0 ? item.product.width : 11,
      height: item.product.height && item.product.height > 0 ? item.product.height : 2,
      length: item.product.length && item.product.length > 0 ? item.product.length : 16,
      quantity: item.quantity
    }));
  }

  getMaxInstallmentsForCart(): number {
    const items = this.cartItems();
    if (items.length === 0) return 1;
    const maxValues = items.map(item => item.product.maxInstallments ?? 12);
    const minValue = Math.min(...maxValues);
    return Math.min(12, Math.max(1, Math.floor(minValue)));
  }

  getNoInterestInstallmentsForCart(): number {
    const items = this.cartItems();
    if (items.length === 0) return 0;
    const max = this.getMaxInstallmentsForCart();
    const noInterestValues = items.map(item => item.product.maxNoInterestInstallments ?? 0);
    const minValue = Math.min(...noInterestValues);
    return Math.min(max, Math.max(0, Math.floor(minValue)));
  }

  getInstallmentValueForCart(): number {
    const max = this.getMaxInstallmentsForCart();
    return Number((this.total() / max).toFixed(2));
  }

  onCheckout() {
    if (this.cartItems().length === 0) {
      this.notificationService.warning('Seu carrinho está vazio!', 'Ops!');
      return;
    }

    // Verificar se todos os vendedores têm frete selecionado
    const sellers = this.itemsBySeller();
    const missingShipping = sellers.some(s => !s.selectedShipping);
    if (missingShipping) {
      this.notificationService.error('Selecione uma opção de frete para todos os vendedores.', 'Erro');
      return;
    }

    this.isLoading = true;

    const itemsToBuy = this.cartItems();
    const shippingData = sellers.map(s => ({
      sellerId: s.sellerId,
      shippingOption: s.selectedShipping
    }));

    // Enviar dados de frete com o checkout
    this.checkoutService.createCheckoutSessionWithShipping(itemsToBuy, shippingData, this.couponCode).subscribe({
      next: (response: any) => {
        if (response.url) {
          this.cartService.clearCart();
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

  validateCoupon() {
    if (!this.couponCode.trim()) {
      this.notificationService.warning('Digite um código de cupom', 'Atenção');
      return;
    }

    this.validatingCoupon.set(true);
    const couponService = inject(CouponService);
    const productIds = this.cartItems().map(item => item.product.id);

    couponService.validateCoupon(this.couponCode, this.total(), productIds)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          if (result.isValid) {
            this.couponValidation.set(result);
            this.couponDiscount.set(result.discountAmount);
            this.notificationService.success(`Cupom aplicado! Desconto: R$ ${result.discountAmount.toFixed(2)}`, 'Sucesso');
          } else {
            this.notificationService.error(result.errorMessage || 'Cupom inválido', 'Erro');
            this.couponValidation.set(null);
            this.couponDiscount.set(0);
          }
          this.validatingCoupon.set(false);
        },
        error: (err) => {
          console.error('Erro ao validar cupom:', err);
          this.notificationService.error('Erro ao validar cupom', 'Erro');
          this.validatingCoupon.set(false);
        }
      });
  }

  removeCoupon() {
    this.couponCode = '';
    this.couponDiscount.set(0);
    this.couponValidation.set(null);
  }

  isCalculating(): boolean {
    return this.itemsBySeller().some(s => s.isCalculating);
  }

  hasAnyShippingCalculating(): boolean {
    return this.itemsBySeller().some(s => s.isCalculating);
  }

  isMissingShipping(): boolean {
    return this.itemsBySeller().some(s => !s.selectedShipping);
  }
}
