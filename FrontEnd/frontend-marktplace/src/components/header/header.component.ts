import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';
import { CartService } from '../../services/cart/cart.service'; // 1. Importar

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  public cartService = inject(CartService);

  currentUser$ = this.authService.currentUser$;

  isMobileMenuOpen = false;
  isCategoryMenuOpen = false;

  categories = [
    { label: 'Decoração', value: 'HomeDecor', icon: '🏠' },
    { label: 'Joias', value: 'Jewelry', icon: '💍' },
    { label: 'Roupas', value: 'Clothing', icon: '👗' },
    { label: 'Arte', value: 'Art', icon: '🎨' },
    { label: 'Brinquedos', value: 'Toys', icon: '🧸' },
    { label: 'Acessórios', value: 'Accessories', icon: '👓' },
    { label: 'Móveis', value: 'Furniture', icon: '🪑' },
    { label: 'Cozinha', value: 'Kitchenware', icon: '🍳' },
    { label: 'Papelaria', value: 'Stationery', icon: '✏️' },
    { label: 'Outros', value: 'Other', icon: '✨' }
  ];

  toggleMobileMenu() {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
    if (!this.isMobileMenuOpen) {
      this.isCategoryMenuOpen = false;
    }
  }

  toggleCategoryMenu() {
    this.isCategoryMenuOpen = !this.isCategoryMenuOpen;
  }

  closeMenus() {
    this.isMobileMenuOpen = false;
    this.isCategoryMenuOpen = false;
  }

  logout() {
    this.authService.logout();
    this.closeMenus();
    this.router.navigate(['/']);
  }
}
