import { Component, DestroyRef, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Product } from '../../models/product/product.model';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';
import { CartService } from '../../services/cart/cart.service';
import { FavoritesService } from '../../services/favorites/favorite.service';
import { AuthService } from '../../services/auth/auth.service';
import { map, take } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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
  private favoritesService = inject(FavoritesService);
  private authService = inject(AuthService);

  private destroyRef = inject(DestroyRef);

  isFavorite$ = this.favoritesService.favoriteProductIds$.pipe(
    takeUntilDestroyed(this.destroyRef),
    map(ids => ids.includes(this.product.id))
  );

  addToCart(event: Event, product: Product) {
    event.stopPropagation();
    this.cartService.addToCart(product);
    console.log('Adicionado ao carrinho:', product.name);
  }

  toggleFavorite(event: Event) {
    event.stopPropagation();

    this.authService.currentUser$.pipe(take(1)).subscribe(user => {
      if (!user) {
        alert('Faça login para salvar seus favoritos!');
        return;
      }

      const isCurrentlyFavorite = this.favoritesService.isFavorite(this.product.id);

      const action = isCurrentlyFavorite
        ? this.favoritesService.removeFromFavorites(this.product.id)
        : this.favoritesService.addToFavorites(this.product.id);

      action.subscribe({
        next: () => {
          console.log(`Produto ${isCurrentlyFavorite ? 'removido' : 'adicionado'} dos favoritos.`);
        },
        error: (err) => {
          console.error("Falha ao atualizar favoritos:", err);
          alert('Erro ao atualizar favoritos. Tente novamente.');
        }
      });
    });
  }
}
