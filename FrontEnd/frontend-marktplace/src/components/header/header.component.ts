import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { FormsModule } from '@angular/forms'; // 1. Importar FormsModule para usar ngModel
import { AuthService } from '../../services/auth/auth.service';
import { CartService } from '../../services/cart/cart.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, FormsModule], // 2. Adicionar FormsModule
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  public cartService = inject(CartService);

  currentUser$ = this.authService.currentUser$;
  searchTerm: string = '';

  isMobileMenuOpen = false;
  isCategoryMenuOpen = false;

  categories = [
    { label: 'Decoração', value: '0', icon: '🏠' },
    { label: 'Joias', value: '1', icon: '💍' },
    { label: 'Roupas', value: '2', icon: '👗' },
    { label: 'Arte', value: '3', icon: '🎨' },
    { label: 'Brinquedos', value: '4', icon: '🧸' },
    { label: 'Acessórios', value: '5', icon: '👓' },
    { label: 'Móveis', value: '6', icon: '🪑' },
    { label: 'Cozinha', value: '7', icon: '🍳' },
    { label: 'Papelaria', value: '8', icon: '✏️' },
    { label: 'Outros', value: '9', icon: '✨' }
  ];

  onSearch() {
    if (this.searchTerm.trim()) {
      this.router.navigate(['/'], { queryParams: { search: this.searchTerm } });
    } else {
      this.router.navigate(['/']);
    }
  }

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
