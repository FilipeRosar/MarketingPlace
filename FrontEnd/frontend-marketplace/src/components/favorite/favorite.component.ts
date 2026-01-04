import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FavoritesService } from '../../services/favorites/favorite.service';
import { ProductService } from '../../services/product/product.service';
import { Product } from '../../models/product/product.model';
import { ProductCardComponent } from '../../components/product-card/product-card.component';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';
import { forkJoin, of } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-favorites',
  standalone: true,
  imports: [CommonModule, RouterLink, ProductCardComponent, LoadingSpinnerComponent],
  templateUrl: './favorite.component.html',
  styles: []
})
export class FavoritesComponent implements OnInit {
  private favoritesService = inject(FavoritesService);
  private productService = inject(ProductService);

  favoriteProducts: Product[] = [];
  isLoading = true;

  ngOnInit() {
    this.loadFavorites();
  }

  loadFavorites() {
    this.isLoading = true;

    // 1. Obtém a lista de IDs dos favoritos
    this.favoritesService.getFavorites().pipe(
      switchMap((ids: string[]) => {
        if (ids.length === 0) return of([]);

        const requests = ids.map(id => this.productService.getProductById(id).pipe(
            catchError(() => of(null)) // Se um produto falhar (ex: deletado), retorna null para não quebrar tudo
        ));
        return forkJoin(requests);
      })
    ).subscribe({
      next: (products: (Product | null)[]) => {
        // Filtra os nulos e atualiza a lista
        this.favoriteProducts = products.filter(p => p !== null) as Product[];
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erro ao carregar favoritos', err);
        this.isLoading = false;
      }
    });
  }
}
