import { Component, Input, inject, DestroyRef, OnInit } from '@angular/core';
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
export class ProductCardComponent implements OnInit {
  @Input({ required: true }) product!: Product;

  private cartService = inject(CartService);
  public favoritesService = inject(FavoritesService);
  private authService = inject(AuthService);
  private destroyRef = inject(DestroyRef);

  isFavorite$ = this.favoritesService.favoriteProductIds$.pipe(
    takeUntilDestroyed(),
    map((ids: string[]) => ids.includes(this.product.id))
  );

  // Variavel local para controle visual imediato (Otimista)
  isFavoriteLocal = false;

  ngOnInit() {
    // Inscreve para atualizar o estado local
    this.isFavorite$.subscribe(isFav => this.isFavoriteLocal = isFav);
  }

  addToCart(event: Event, product: Product) {
    event.stopPropagation();
    this.cartService.addToCart(product);
  }

  toggleFavorite(event: Event) {
    event.stopPropagation();

    if (!this.authService.currentUserValue) {
      alert('Faça login para salvar seus favoritos!');
      return;
    }

    this.isFavoriteLocal = !this.isFavoriteLocal;

    const action = this.isFavoriteLocal
      ? this.favoritesService.addToFavorites(this.product.id)
      : this.favoritesService.removeFromFavorites(this.product.id);

    action.subscribe({
      error: (err) => {
        this.isFavoriteLocal = !this.isFavoriteLocal;
        console.error("Falha ao atualizar favoritos:", err);
      }
    });
  }
}
