import { Component, inject, OnInit, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartService } from '../../../services/cart/cart.service';
import { CurrencyBrPipe } from '../../../shared/pipes/currency-br-pipe';
import { ProductCardComponent } from '../../../components/product-card/product-card.component';
import { OrderService } from '../../../services/order/order.service';
import { ProductService } from '../../../services/product/product.service';
import { Product } from '../../../models/product/product.model';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, map, switchMap, take } from 'rxjs/operators';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyBrPipe, ProductCardComponent],
  templateUrl: './cart.html',
  styles: [`
  input[type="number"]::-webkit-inner-spin-button,
  input[type="number"]::-webkit-outer-spin-button {
    -webkit-appearance: none;
    margin: 0;
  }

  .animate-fade-in {
    animation: fadeIn 0.6s ease-out;
  }

  @keyframes fadeIn {
    from { opacity: 0; transform: translateY(10px); }
    to { opacity: 1; transform: translateY(0); }
  }
`]
})
export class CartComponent implements OnInit {
  cartService = inject(CartService);
  private orderService = inject(OrderService);
  private productService = inject(ProductService);

  cartItems = this.cartService.cartItems;
  total = this.cartService.total;
  suggestedProducts: Product[] = [];
  isLoadingSuggestions = false;
  private lastCartKey = '';

  ngOnInit() {
    this.loadSuggestions();

    effect(() => {
      const cartKey = this.cartItems()
        .map(item => `${item.product.id}:${item.quantity}`)
        .sort()
        .join('|');
      if (!cartKey || cartKey === this.lastCartKey) return;
      this.lastCartKey = cartKey;
      this.loadSuggestions();
    });
  }

  updateQuantity(productId: string, quantity: number) {
    if (quantity > 0) {
      this.cartService.updateQuantity(productId, quantity);
    }
  }

  removeItem(productId: string) {
    this.cartService.removeFromCart(productId);
  }

  private loadSuggestions() {
    if (this.isLoadingSuggestions) return;
    this.isLoadingSuggestions = true;
    let purchasedIds: string[] = [];

    this.orderService.getMyOrders().pipe(
      take(1),
      catchError(() => of([])),
      switchMap((orders) => {
        purchasedIds = Array.from(new Set(orders.flatMap(order => order.items.map(item => item.productId))));
        if (purchasedIds.length === 0) {
          const cartProductIds = Array.from(new Set(this.cartItems().map(item => item.product.id)));
          if (cartProductIds.length === 0) {
            return of({ tags: new Set<string>(), purchasedIds });
          }

          return forkJoin(
            cartProductIds.map(id =>
              this.productService.getProductById(id).pipe(catchError(() => of(null)))
            )
          ).pipe(
            map(products => {
              const tags = new Set<string>();
              for (const product of products) {
                if (product?.tags?.length) {
                  product.tags.forEach(tag => tags.add(tag.toLowerCase()));
                }
              }
              return { tags, purchasedIds };
            })
          );
        }

        return forkJoin(
          purchasedIds.map(id =>
            this.productService.getProductById(id).pipe(catchError(() => of(null)))
          )
        ).pipe(
          map(products => {
            const tags = new Set<string>();
            for (const product of products) {
              if (product?.tags?.length) {
                product.tags.forEach(tag => tags.add(tag.toLowerCase()));
              }
            }
            return { tags, purchasedIds };
          })
        );
      }),
      switchMap(({ tags, purchasedIds: boughtIds }) => {
        const tagList = Array.from(tags).slice(0, 5);
        if (tagList.length === 0) {
          return of({ products: [] as Product[], purchasedIds: boughtIds });
        }

        return forkJoin(
          tagList.map(tag =>
            this.productService.getAllProducts(1, 8, tag).pipe(
              catchError(() => of({ data: [] }))
            )
          )
        ).pipe(
          map(responses => {
            const merged: Product[] = [];
            for (const response of responses) {
              const items = Array.isArray(response) ? response : (response?.data ?? []);
              merged.push(...items);
            }
            return { products: merged, purchasedIds: boughtIds };
          })
        );
      }),
      map(({ products, purchasedIds: boughtIds }) => {
        const cartIds = new Set(this.cartItems().map(item => item.product.id));
        const purchasedSet = new Set(boughtIds);
        const unique = new Map<string, Product>();

        for (const product of products) {
          if (!product?.id) continue;
          if (cartIds.has(product.id) || purchasedSet.has(product.id)) continue;
          if (!unique.has(product.id)) {
            unique.set(product.id, product);
          }
        }

        return Array.from(unique.values()).slice(0, 8);
      }),
      finalize(() => {
        this.isLoadingSuggestions = false;
      })
    ).subscribe((products) => {
      this.suggestedProducts = products;
    });
  }
}
