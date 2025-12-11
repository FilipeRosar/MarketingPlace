import { Injectable, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Product } from '../../models/product/product.model';
import { AuthService } from '../auth/auth.service';
import { environment } from '../../environments/environment';
import { NotificationService } from '../notification/notification.service';
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
  private notificationService = inject(NotificationService);
  private platformId = inject(PLATFORM_ID);
  private apiUrl = `${environment.apiUrl}/carts`;

   cartItems = signal<CartItem[]>([]);

  cartCount = computed(() => this.cartItems().reduce((acc, i) => acc + i.quantity, 0));
  total = computed(() => this.cartItems().reduce((acc, item) => acc + (item.product.price * item.quantity), 0));

  constructor() {
    this.loadCart();
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.syncCartWithServer();
      } else {
        this.clearLocalCart();
      }
    });
  }

  async addToCart(product: Product) {
    // 1. Snapshot do estado antes da alteração (para rollback)
    const previousItems = this.cartItems();

    // 2. Atualização Otimista (UI atualiza na hora)
    this.updateLocalState(product, 1);
    this.notificationService.success(`${product.name} adicionado!`);

    // 3. Persistência no Backend
    if (this.authService.currentUserValue) {
      try {
        await firstValueFrom(this.http.post(`${this.apiUrl}/add`, {
          productId: product.id,
          quantity: 1
        }));
      } catch (err) {
        console.error('Erro ao salvar carrinho no servidor', err);

        // 4. ROLLBACK: Reverte se der erro
        this.cartItems.set(previousItems);
        this.saveLocal();
        this.notificationService.error('Erro ao salvar no carrinho. Tente novamente.');
      }
    }
  }

  async updateQuantity(productId: string, quantity: number) {
    const previousItems = this.cartItems();

    this.cartItems.update(items =>
      items.map(item => item.product.id === productId ? { ...item, quantity } : item)
    );
    this.saveLocal();

    if (this.authService.currentUserValue) {
      this.http.put(`${this.apiUrl}/update`, { productId, quantity }).subscribe({
        error: (err) => {
          console.error('Erro ao atualizar quantidade', err);
          // Rollback
          this.cartItems.set(previousItems);
          this.saveLocal();
          this.notificationService.error('Não foi possível atualizar a quantidade.');
        }
      });
    }
  }

  async removeFromCart(productId: string) {
    const previousItems = this.cartItems();

    this.cartItems.set(this.cartItems().filter(i => i.product.id !== productId));
    this.saveLocal();

    if (this.authService.currentUserValue) {
      this.http.delete(`${this.apiUrl}/remove/${productId}`).subscribe({
        error: (err) => {
            console.error('Erro ao remover item', err);
            // Rollback
            this.cartItems.set(previousItems);
            this.saveLocal();
            this.notificationService.error('Erro ao remover item.');
        }
      });
    }
  }

  // --- Helpers ---

  public clearCart() {
    this.clearLocalCart();
    if (this.authService.currentUserValue) {
      this.http.delete(`${this.apiUrl}/clear`).subscribe({
        error: (err) => console.warn('Erro ao limpar carrinho no servidor', err)
      });
    }
  }

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
      // Atualiza apenas localmente, sem chamar a API de novo (addToCart já chama ou quem chamou updateLocalState)
      // Nota: Se updateLocalState for chamado de addToCart, ele só atualiza o sinal.
      // Se for chamado de outro lugar, precisa cuidar.
      // Aqui, simplificamos: addToCart chama a API de ADD. Se já existe, a API de ADD deve saber somar ou retornar ok.
      // No seu Backend CartService.cs, AddItemAsync soma a quantidade se já existe. Então está correto.

      this.cartItems.update(items =>
          items.map(item => item.product.id === product.id ? { ...item, quantity: item.quantity + qty } : item)
      );
    } else {
      this.cartItems.set([...current, { product, quantity: qty }]);
    }
    this.saveLocal();
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
            this.cartItems.set([]);
            this.saveLocal();
        }
      },
      error: (err) => console.error('Erro ao sincronizar carrinho:', err)
    });
  }
}
