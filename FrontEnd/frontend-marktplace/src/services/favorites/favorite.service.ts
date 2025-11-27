import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { Product } from '../../models/product/product.model';

@Injectable({
  providedIn: 'root'
})
export class FavoritesService {
  private apiUrl = `${environment.apiUrl}/favorites`;

  private favoriteProductIdsSubject = new BehaviorSubject<string[]>([]);
  public favoriteProductIds$ = this.favoriteProductIdsSubject.asObservable();

  private http = inject(HttpClient);

  constructor() {
    // Em um app real, aqui chamaríamos a API para carregar a lista ao fazer login
  }

  isFavorite(productId: string): boolean {
    return this.favoriteProductIdsSubject.value.includes(productId);
  }


  addToFavorites(productId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/add`, { productId }).pipe(
      tap(() => {
        const current = this.favoriteProductIdsSubject.value;
        this.favoriteProductIdsSubject.next([...current, productId]);
      })
    );
  }

  removeFromFavorites(productId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${productId}`).pipe(
      tap(() => {
        const current = this.favoriteProductIdsSubject.value;
        this.favoriteProductIdsSubject.next(current.filter(id => id !== productId));
      })
    );
  }

  setFavorites(productIds: string[]) {
    this.favoriteProductIdsSubject.next(productIds);
  }
}
