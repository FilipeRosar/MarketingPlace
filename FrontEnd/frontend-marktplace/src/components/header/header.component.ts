import { Component, inject, OnInit, OnDestroy, HostListener, Output, EventEmitter, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth/auth.service';
import { CartService } from '../../services/cart/cart.service';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private router = inject(Router);
  public cartService = inject(CartService);
  private platformId = inject(PLATFORM_ID);

  currentUser$ = this.authService.currentUser$;

  searchTerm = '';
  isMobileMenuOpen = false;
  isCategoryMenuOpen = false;
  isUserMenuOpen = false;
  private categoryTimeout: any;
  private routerSub!: Subscription;

  @Output() toggleDark = new EventEmitter<void>();

  categories = [
    { label: 'Decoração',     value: '0', icon: 'Home' },
    { label: 'Joias',         value: '1', icon: 'Jewelry' },
    { label: 'Roupas',        value: '2', icon: 'Dress' },
    { label: 'Arte',          value: '3', icon: 'Art' },
    { label: 'Brinquedos',    value: '4', icon: 'Toys' },
    { label: 'Acessórios',    value: '5', icon: 'Glasses' },
    { label: 'Móveis',        value: '6', icon: 'Chair' },
    { label: 'Cozinha',       value: '7', icon: 'Cooking' },
    { label: 'Papelaria',     value: '8', icon: 'Pencil' },
    { label: 'Outros',        value: '9', icon: 'Sparkles' }
  ];

  ngOnInit(): void {
    this.routerSub = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.closeMenus();
      // Limpa busca visual se não tiver query
      const urlTree = this.router.parseUrl(this.router.url);
      if (!urlTree.queryParams['search']) {
        this.searchTerm = '';
      }
    });
  }

  ngOnDestroy(): void {
    if (this.categoryTimeout) clearTimeout(this.categoryTimeout);
    this.routerSub?.unsubscribe();
  }

  onSearch(): void {
    const term = this.searchTerm.trim();
    if (term) {
      this.router.navigate(['/categorias'], { queryParams: { search: term } });
    } else {
      this.router.navigate(['/categorias']);
    }
    this.closeMenus();
  }

  onMouseEnterCategory() {
    if (this.categoryTimeout) clearTimeout(this.categoryTimeout);
    this.isCategoryMenuOpen = true;
  }

  onMouseLeaveCategory() {
    this.categoryTimeout = setTimeout(() => {
      this.isCategoryMenuOpen = false;
    }, 250);
  }

  toggleMobileMenu() {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
    if (this.isMobileMenuOpen) {
      this.isCategoryMenuOpen = false;
      this.isUserMenuOpen = false;
    }
  }

  toggleUserMenu() {
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  closeMenus() {
    this.isMobileMenuOpen = false;
    this.isCategoryMenuOpen = false;
    this.isUserMenuOpen = false;
  }

  onToggleDark() {
    this.toggleDark.emit();
  }
  toggleCategoryMenu(): void {
  this.isCategoryMenuOpen = !this.isCategoryMenuOpen;

  if (this.isCategoryMenuOpen && this.isMobileMenuOpen) {
    this.isUserMenuOpen = false;
  }
}
  logout() {
    this.authService.logout();
    this.closeMenus();
    this.router.navigate(['/']);
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event) {
    if (!isPlatformBrowser(this.platformId)) return;
    const target = event.target as HTMLElement;
    if (!target.closest('header')) {
      this.closeMenus();
    }
  }
}
