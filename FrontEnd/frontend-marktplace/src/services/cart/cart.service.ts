import { Injectable, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Product } from '../../models/product/product.model';
import { AuthService } from '../auth/auth.service';
import { environment } from '../../environments/environment';
import { firstValueFrom } from 'rxjs';

export interface CartItem {
  product: Product;
  quantity: number;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private platformId = inject(PLATFORM_ID);
  private apiUrl = `${environment.apiUrl}/carts`;

  cartItems = signal<CartItem[]>([]);

  count = computed(() => this.cartItems().reduce((acc, item) => acc + item.quantity, 0));
  total = computed(() => this.cartItems().reduce((acc, item) => acc + (item.product.price * item.quantity), 0));

  constructor() {
    // Carrega o carrinho salvo no navegador (se houver) ao iniciar
    this.loadCart();

    // Escuta mudanças no login/logout
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        // Se o usuário logou, sincroniza com o que está no banco de dados
        this.syncCartWithServer();
      } else {
        // CORREÇÃO: Se o usuário deslogou, LIMPA o carrinho local
        this.clearLocalCart();
      }
    });
  }

  async addToCart(product: Product) {
    this.updateLocalState(product, 1);

    if (this.authService.currentUserValue) {
      try {
        await firstValueFrom(this.http.post(`${this.apiUrl}/add`, {
          productId: product.id,
          quantity: 1
        }));
      } catch (err) {
        console.error('Erro ao salvar carrinho no servidor', err);
      }
    }
  }

  async updateQuantity(productId: string, quantity: number) {
    this.cartItems.update(items =>
      items.map(item => item.product.id === productId ? { ...item, quantity } : item)
    );
    this.saveLocal();

    if (this.authService.currentUserValue) {
      this.http.put(`${this.apiUrl}/update`, { productId, quantity }).subscribe();
    }
  }

  async removeFromCart(productId: string) {
    this.cartItems.set(this.cartItems().filter(i => i.product.id !== productId));
    this.saveLocal();

    if (this.authService.currentUserValue) {
      this.http.delete(`${this.apiUrl}/remove/${productId}`).subscribe();
    }
  }

  // --- Helpers ---

  // Limpa o carrinho da memória e do localStorage
  private clearLocalCart() {
    this.cartItems.set([]);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('cart');
    }
  }

  private updateLocalState(product: Product, qty: number) {
    const current = this.cartItems();
    const existing = current.find(i => i.product.id === product.id);

    if (existing) {
      this.updateQuantity(product.id, existing.quantity + qty);
    } else {
      this.cartItems.set([...current, { product, quantity: qty }]);
      this.saveLocal();
    }
  }

  private saveLocal() {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('cart', JSON.stringify(this.cartItems()));
    }
  }

  private loadCart() {
    if (isPlatformBrowser(this.platformId)) {
      const saved = localStorage.getItem('cart');
      if (saved) {
        try {
            this.cartItems.set(JSON.parse(saved));
        } catch {
            this.cartItems.set([]);
        }
      }
    }
  }

  private syncCartWithServer() {
    this.http.get<any>(this.apiUrl).subscribe({
      next: (cartDto) => {
        if (cartDto && cartDto.items) {
            const items: CartItem[] = cartDto.items.map((i: any) => ({
                product: {
                    id: i.productId,
                    name: i.productName,
                    price: i.price,
                    imageUrl: i.productImage,
                    // Preenchemos o resto com dados padrão pois o DTO do carrinho é simplificado
                    description: '',
                    stockQuantity: 99,
                    category: '',
                    sellerId: '',
                    sellerName: '',
                    averageRating: 0,
                    totalRatings: 0,
                    images: [],
                    seller: { id: '', name: '', email: '', phone: '' }
                },
                quantity: i.quantity
            }));
            this.cartItems.set(items);
            this.saveLocal();
        } else {
            // Se o servidor retornar vazio, garantimos que o local também fique vazio
            this.cartItems.set([]);
            this.saveLocal();
        }
      },
      error: () => {
        // Se der erro ao sincronizar (ex: token expirado), não faz nada ou limpa
      }
    });
  }
}
