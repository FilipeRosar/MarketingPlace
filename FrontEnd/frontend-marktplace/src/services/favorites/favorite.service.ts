import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth/auth.service'; // 1. Importar AuthService

@Injectable({
  providedIn: 'root'
})
export class FavoritesService {
  private apiUrl = `${environment.apiUrl}/favorites`;
  private http = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);
  private authService = inject(AuthService); // 2. Injetar AuthService

  private favoriteProductIdsSubject = new BehaviorSubject<string[]>([]);
  public favoriteProductIds$ = this.favoriteProductIdsSubject.asObservable();

  constructor() {
     this.loadInitialFavorites();
  }

  loadInitialFavorites() {
      if (isPlatformBrowser(this.platformId)) {
          if (localStorage.getItem('token')) {
              this.getFavorites().subscribe({
                  error: (err) => {
                      console.error('Erro ao carregar favoritos na inicialização:', err);
                      if (err.status === 401) {
                          this.authService.logout();
                      }
                  }
              });
          }
      }
  }

  getFavorites(): Observable<string[]> {
    return this.http.get<string[]>(this.apiUrl).pipe(
      tap(ids => this.favoriteProductIdsSubject.next(ids))
    );
  }

  isFavorite(productId: string): boolean {
    return this.favoriteProductIdsSubject.value.includes(productId);
  }

  addToFavorites(productId: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/add`, { productId }).pipe(
      tap(() => {
        const current = this.favoriteProductIdsSubject.value;
        if (!current.includes(productId)) {
            this.favoriteProductIdsSubject.next([...current, productId]);
        }
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
}
