import { Component, Input, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Product } from '../../models/product/product.model';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';
import { CartService } from '../../services/cart/cart.service';
import { FavoritesService } from '../../services/favorites/favorite.service';
import { AuthService } from '../../services/auth/auth.service';
import { NotificationService } from '../../services/notification/notification.service';
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

  isFavorite = false;
  private favSub?: Subscription;

  ngOnInit() {
    // Escuta a lista global de favoritos.
    // Se o ID deste produto estiver na lista, marca como favorito.
    this.favSub = this.favoritesService.favoriteProductIds$.subscribe(ids => {
      this.isFavorite = ids.includes(this.product.id);
    });
  }

  ngOnDestroy() {
    if (this.favSub) this.favSub.unsubscribe();
  }

  addToCart(event: Event, product: Product) {
    event.stopPropagation();
    this.cartService.addToCart(product);
    // Feedback visual elegante
    this.notificationService.success(`"${product.name}" adicionado ao carrinho!`);
  }

  toggleFavorite(event: Event) {
    event.stopPropagation();

    // Verifica se está logado
    const user = this.authService.currentUserValue;
    if (!user) {
      // Alerta amigável em vez de popup nativo
      this.notificationService.info('Faça login para salvar seus favoritos.', 'Atenção');
      return;
    }

    // 1. Atualização Otimista (Muda a cor imediatamente para o usuário não esperar)
    const previousState = this.isFavorite;
    this.isFavorite = !this.isFavorite;

    // 2. Chama o serviço para persistir
    const action = previousState
      ? this.favoritesService.removeFromFavorites(this.product.id) // Se já era favorito, remove
      : this.favoritesService.addToFavorites(this.product.id);     // Se não era, adiciona

    action.pipe(take(1)).subscribe({
      next: () => {
        // Sucesso: Feedback opcional (muitos apps não notificam "like", só mudam a cor)
        if (!previousState) {
             this.notificationService.success('Produto salvo nos favoritos!');
        }
      },
      error: (err) => {
        // Erro: Reverte a mudança visual
        console.error("Erro ao favoritar:", err);
        this.isFavorite = previousState;
        this.notificationService.error('Não foi possível atualizar favoritos. Tente novamente.');
      }
    });
  }
}
