import { Component, inject, OnInit, OnDestroy, HostListener, Output, EventEmitter, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, NavigationEnd } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth/auth.service';
import { CartService } from '../../services/cart/cart.service';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    FormsModule
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent implements OnInit, OnDestroy {

  // Services
  private authService = inject(AuthService);
  private router = inject(Router);
  public cartService = inject(CartService);
  private platformId = inject(PLATFORM_ID);

  // Observables & Data
  currentUser$ = this.authService.currentUser$;

  // Estados de Menu
  isCategoryMenuOpen = false;
  isCategoryHover = false;
  isUserMenuOpen = false;
  isMobileMenuOpen = false;

  // Estado para controlar qual categoria está expandida no Mobile
  expandedCategoryMobile: string | null = null;

  private categoryTimeout: any;
  private routerSub!: Subscription;

  // Form controls
  searchTerm: string = '';

  // Categorias com Subcategorias
  categories = [
    {
      label: 'Decoração', value: '0', icon: '🏠',
      subItems: [
        { label: 'Vasos & Cachepots', value: '0-1' },
        { label: 'Quadros & Telas', value: '0-2' },
        { label: 'Almofadas', value: '0-3' },
        { label: 'Velas e Aromas', value: '0-4' }
      ]
    },
    {
      label: 'Joias', value: '1', icon: '💍',
      subItems: [
        { label: 'Colares', value: '1-1' },
        { label: 'Brincos', value: '1-2' },
        { label: 'Anéis', value: '1-3' },
        { label: 'Pulseiras', value: '1-4' }
      ]
    },
    {
      label: 'Roupas', value: '2', icon: '👗',
      subItems: [
        { label: 'Vestidos', value: '2-1' },
        { label: 'Blusas de Crochê', value: '2-2' },
        { label: 'Saias', value: '2-3' }
      ]
    },
    {
      label: 'Arte', value: '3', icon: '🎨',
      subItems: [
        { label: 'Esculturas', value: '3-1' },
        { label: 'Pinturas', value: '3-2' }
      ]
    },
    { label: 'Brinquedos', value: '4', icon: '🧸', subItems: [] },
    { label: 'Acessórios', value: '5', icon: '👓', subItems: [{ label: 'Bolsas', value: '5-1' }, { label: 'Chapéus', value: '5-2' }] },
    { label: 'Móveis', value: '6', icon: '🪑', subItems: [] },
    { label: 'Cozinha', value: '7', icon: '🍳', subItems: [{ label: 'Panos de Prato', value: '7-1' }, { label: 'Tábuas', value: '7-2' }] },
    { label: 'Papelaria', value: '8', icon: '✏️', subItems: [] },
    { label: 'Outros', value: '9', icon: '✨', subItems: [] }
  ];

  @Output() toggleDark = new EventEmitter<void>();

  ngOnInit(): void {
    this.routerSub = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      if (this.router.url === '/' || this.router.url === '/products') {
        const tree = this.router.parseUrl(this.router.url);
        if (!tree.queryParams['search'] && this.searchTerm) {
           this.searchTerm = '';
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.closeMenus();
    if (this.routerSub) this.routerSub.unsubscribe();
  }

  onSearch(): void {
    const term = this.searchTerm.trim();
    if (term) {
      this.router.navigate(['/products'], { queryParams: { search: term } });
    } else {
      this.router.navigate(['/']);
    }
    this.closeMenus();
  }

  // --- MENUS CONTROLS ---

  toggleMobileMenu(): void {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;
    if (this.isMobileMenuOpen) {
       this.isCategoryMenuOpen = false;
       this.isUserMenuOpen = false;
    }
  }

  // NOVO: Toggle para subcategorias no mobile
  toggleMobileSubCategory(categoryValue: string): void {
    if (this.expandedCategoryMobile === categoryValue) {
      this.expandedCategoryMobile = null; // Fecha se já estiver aberto
    } else {
      this.expandedCategoryMobile = categoryValue;
    }
  }

  toggleUserMenu(): void {
    this.isUserMenuOpen = !this.isUserMenuOpen;
    if (this.isUserMenuOpen) {
       this.isCategoryMenuOpen = false;
    }
  }

  onToggleDark() {
    this.toggleDark.emit();
  }

  onMouseEnterCategory() {
    if (this.categoryTimeout) clearTimeout(this.categoryTimeout);
    this.isCategoryMenuOpen = true;
  }

  onMouseLeaveCategory() {
    this.categoryTimeout = setTimeout(() => {
      this.isCategoryMenuOpen = false;
    }, 200);
  }

  toggleCategoryMenu(): void {
    this.isCategoryMenuOpen = !this.isCategoryMenuOpen;
  }

  closeCategoryMenu(): void {
    this.isCategoryMenuOpen = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!isPlatformBrowser(this.platformId)) return;
    const target = event.target as HTMLElement;
    if (!target.closest('.relative') && !target.closest('.prevent-close')) {
      this.isCategoryMenuOpen = false;
      this.isUserMenuOpen = false;
    }
  }

  closeMenus(): void {
    this.isMobileMenuOpen = false;
    this.isCategoryMenuOpen = false;
    this.isUserMenuOpen = false;
    this.expandedCategoryMobile = null;
  }

  logout(): void {
    this.authService.logout();
    this.closeMenus();
    this.router.navigate(['/']).then(() => {
      if (isPlatformBrowser(this.platformId)) {
         window.scrollTo(0, 0);
      }
    });
  }
}
