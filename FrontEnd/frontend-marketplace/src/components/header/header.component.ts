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
  isDesktopCategoryOpen = false; // Separado para Desktop
  isMobileCategoryOpen = false;  // Separado para Mobile
  isUserMenuOpen = false;
  isMobileMenuOpen = false;

  private categoryTimeout: any;
  private routerSub!: Subscription;

  // Form controls
  searchTerm: string = '';

  // Categorias (Labels limpas, ícones removidos do TS pois agora estarão no HTML)
  categories = [
    { label: 'Decoração', value: '0' },
    { label: 'Joias', value: '1' },
    { label: 'Roupas', value: '2' },
    { label: 'Arte', value: '3' },
    { label: 'Brinquedos', value: '4' },
    { label: 'Acessórios', value: '5' },
    { label: 'Móveis', value: '6' },
    { label: 'Cozinha', value: '7' },
    { label: 'Papelaria', value: '8' },
    { label: 'Outros', value: '9' }
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
    // Resetar estados internos quando fecha ou abre o menu principal
    if (!this.isMobileMenuOpen) {
       this.isMobileCategoryOpen = false;
    }
    this.isUserMenuOpen = false;
    this.isDesktopCategoryOpen = false;
  }

  // Mobile Category Toggle
  toggleMobileCategoryMenu(): void {
    this.isMobileCategoryOpen = !this.isMobileCategoryOpen;
  }

  toggleUserMenu(): void {
    this.isUserMenuOpen = !this.isUserMenuOpen;
    if (this.isUserMenuOpen) {
       this.isDesktopCategoryOpen = false;
    }
  }

  onToggleDark() {
    this.toggleDark.emit();
  }

  // Desktop Hover Logic
  onMouseEnterCategory() {
    if (this.categoryTimeout) clearTimeout(this.categoryTimeout);
    this.isDesktopCategoryOpen = true;
  }

  onMouseLeaveCategory() {
    this.categoryTimeout = setTimeout(() => {
      this.isDesktopCategoryOpen = false;
    }, 200);
  }

  toggleDesktopCategoryMenu(): void {
    this.isDesktopCategoryOpen = !this.isDesktopCategoryOpen;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!isPlatformBrowser(this.platformId)) return;
    const target = event.target as HTMLElement;

    // Fechar menu de usuário se clicar fora
    if (this.isUserMenuOpen && !target.closest('.prevent-close')) {
      this.isUserMenuOpen = false;
    }
  }

  closeMenus(): void {
    this.isMobileMenuOpen = false;
    this.isDesktopCategoryOpen = false;
    this.isMobileCategoryOpen = false;
    this.isUserMenuOpen = false;
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
