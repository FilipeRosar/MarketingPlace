import { Component, Input, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Product } from '../../models/product/product.model';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';
import { CartService } from '../../services/cart/cart.service';
import { FavoritesService } from '../../services/favorites/favorite.service';
import { AuthService } from '../../services/auth/auth.service';
import { NotificationService } from '../../services/notification/notification.service';
import { EventTrackingService } from '../../services/analytics/event-tracking.service';
import { Subscription } from 'rxjs';
import { take } from 'rxjs/operators';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, CurrencyBrPipe, RouterLink],
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.css'
})
export class ProductCardComponent implements OnInit, OnDestroy {
  @Input({ required: true }) product!: Product;

  private cartService = inject(CartService);
  private favoritesService = inject(FavoritesService);
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);
  private eventTrackingService = inject(EventTrackingService);

  isFavorite = false;

  displayImage: string | null = null;

  private favSub?: Subscription;

  ngOnInit() {
    this.eventTrackingService.trackProductView(this.product);
    
    this.favSub = this.favoritesService.favoriteProductIds$.subscribe(ids => {
      this.isFavorite = ids.includes(this.product.id);
    });

    this.normalizeImage();
  }

  // Função para garantir que temos uma URL válida
  normalizeImage() {
    if (this.product.images && this.product.images.length > 0) {
      const firstImage = this.product.images[0];
      // Verifica se é string ou objeto {url: ...}
      this.displayImage = typeof firstImage === 'string' ? firstImage : (firstImage as any).url;
    }
    else if (this.product.imageUrl) {
      this.displayImage = this.product.imageUrl;
    }
  }
  private getDisplayPrice(product: Product): number {
    if (product.salePrice && product.salePrice > 0 && product.salePrice < product.price) {
      return product.salePrice;
    }
    return product.price;
  }

  getMaxInstallments(product: Product): number {
    const max = product.maxInstallments ?? 12;
    return Math.min(12, Math.max(1, Math.floor(max)));
  }

  getNoInterestInstallments(product: Product): number {
    const max = this.getMaxInstallments(product);
    const noInterest = product.maxNoInterestInstallments ?? 0;
    return Math.min(max, Math.max(0, Math.floor(noInterest)));
  }

  getInstallmentValue(product: Product): number {
    const price = this.getDisplayPrice(product);
    const max = this.getMaxInstallments(product);
    return Number((price / max).toFixed(2));
  }
  ngOnDestroy() {
    if (this.favSub) this.favSub.unsubscribe();
  }

  addToCart(event: Event, product: Product) {
    event.stopPropagation();
    const productToAdd = { ...product, imageUrl: this.displayImage || product.imageUrl };
    this.cartService.addToCart(productToAdd);
    
    this.eventTrackingService.trackAddToCart(product, 1);
    
    this.notificationService.success(`"${product.name}" adicionado ao carrinho!`);
  }

  toggleFavorite(event: Event) {
    event.stopPropagation();
    const user = this.authService.currentUserValue;

    if (!user) {
      this.notificationService.info('Faça login para salvar seus favoritos.', 'Atenção');
      return;
    }

    const previousState = this.isFavorite;
    this.isFavorite = !this.isFavorite;

    const action = previousState
      ? this.favoritesService.removeFromFavorites(this.product.id)
      : this.favoritesService.addToFavorites(this.product.id);

    action.pipe(take(1)).subscribe({
      next: () => {
        if (!previousState) {
          this.eventTrackingService.trackCustomEvent('add_to_wishlist', {
            productId: this.product.id,
            productName: this.product.name,
            price: this.product.price
          });
          this.notificationService.success('Produto salvo nos favoritos!');
        } else {
          this.eventTrackingService.trackCustomEvent('remove_from_wishlist', {
            productId: this.product.id,
            productName: this.product.name
          });
        }
      },
      error: (err) => {
        console.error("Erro ao favoritar:", err);
        this.isFavorite = previousState;
        this.notificationService.error('Erro ao atualizar favoritos.');
        
        this.eventTrackingService.trackError('Favorite toggle failed', 'FavoriteError', 'toggleFavorite');
      }
    });
  }
}
