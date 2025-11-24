import { Injectable, signal, computed, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Product } from '../../models/product/product.model';

export interface CartItem {
  product: Product;
  quantity: number;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  // Signal para armazenar os itens (Reativo e Performático)
  cartItems = signal<CartItem[]>([]);

  // Computed Values (Calculados automaticamente quando cartItems muda)
  count = computed(() => this.cartItems().reduce((acc, item) => acc + item.quantity, 0));
  total = computed(() => this.cartItems().reduce((acc, item) => acc + (item.product.price * item.quantity), 0));

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    this.loadCart();
  }

  addToCart(product: Product) {
    const currentItems = this.cartItems();
    const existingItem = currentItems.find(i => i.product.id === product.id);

    if (existingItem) {
      // Se já existe, atualiza a quantidade
      this.updateQuantity(product.id, existingItem.quantity + 1);
    } else {
      // Se não, adiciona novo
      this.cartItems.set([...currentItems, { product, quantity: 1 }]);
    }

    this.saveCart();
  }

  removeFromCart(productId: string) {
    this.cartItems.set(this.cartItems().filter(i => i.product.id !== productId));
    this.saveCart();
  }

  updateQuantity(productId: string, quantity: number) {
    this.cartItems.update(items =>
      items.map(item =>
        item.product.id === productId ? { ...item, quantity } : item
      )
    );
    this.saveCart();
  }

  clearCart() {
    this.cartItems.set([]);
    this.saveCart();
  }

  // Persistência no LocalStorage
  private saveCart() {
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
}
