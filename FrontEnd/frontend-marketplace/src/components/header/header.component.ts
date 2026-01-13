import { Component, inject, OnInit, OnDestroy, HostListener, Output, EventEmitter, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, NavigationEnd } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth/auth.service';
import { CartService } from '../../services/cart/cart.service';
import { ProductService } from '../../services/product/product.service';
import { SellerService } from '../../services/seller/seller.service';
import { ChatService } from '../../services/chat/chat.service';
import { Subscription, Subject, forkJoin, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, filter, switchMap, takeUntil } from 'rxjs/operators';

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
  private productService = inject(ProductService);
  private sellerService = inject(SellerService);
  private chatService = inject(ChatService);

  // Observables & Data
  currentUser$ = this.authService.currentUser$;

  // Estados de Menu
  isDesktopCategoryOpen = false; // Separado para Desktop
  isMobileCategoryOpen = false;  // Separado para Mobile
  isUserMenuOpen = false;
  isMobileMenuOpen = false;
  isSearchOpen = false;
  isSearchLoading = false;

  private categoryTimeout: any;
  private routerSub!: Subscription;
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  // Form controls
  searchTerm: string = '';
  searchSuggestionsProducts: any[] = [];
  searchSuggestionsSellers: any[] = [];
  chatRequestCount = 0;

  // Categorias e subcategorias (ASCII para evitar problemas de encoding)
  categories = [
    {
      label: 'Decoracao',
      value: '0',
      subcategories: [
        'Quadros e Placas',
        'Velas artesanais',
        'Vasos e Cachepos',
        'Macrame',
        'Objetos de mesa',
        'Esculturas decorativas',
        'Iluminacao artesanal',
        'Almofadas e Texteis',
        'Decoracao infantil',
        'Decoracao religiosa'
      ]
    },
    {
      label: 'Joias',
      value: '1',
      subcategories: [
        'Aneis',
        'Colares',
        'Pulseiras',
        'Brincos',
        'Pingentes',
        'Joias em prata',
        'Joias em ouro',
        'Pedras naturais',
        'Joias personalizadas',
        'Joias minimalistas'
      ]
    },
    {
      label: 'Roupas',
      value: '2',
      subcategories: [
        'Camisetas',
        'Vestidos',
        'Moda feminina',
        'Moda masculina',
        'Moda infantil',
        'Roupas personalizadas',
        'Bordados',
        'Croche e Trico',
        'Moda sustentavel',
        'Fantasias'
      ]
    },
    {
      label: 'Arte',
      value: '3',
      subcategories: [
        'Pinturas',
        'Ilustracoes',
        'Gravuras',
        'Arte digital',
        'Esculturas',
        'Arte abstrata',
        'Arte realista',
        'Arte contemporanea',
        'Posters artisticos',
        'Artes autorais'
      ]
    },
    {
      label: 'Brinquedos',
      value: '4',
      subcategories: [
        'Brinquedos educativos',
        'Brinquedos de madeira',
        'Bonecas artesanais',
        'Amigurumi',
        'Jogos pedagogicos',
        'Quebra-cabecas',
        'Brinquedos sensoriais',
        'Brinquedos infantis',
        'Brinquedos personalizados'
      ]
    },
    {
      label: 'Acessorios',
      value: '5',
      subcategories: [
        'Bolsas',
        'Carteiras',
        'Mochilas',
        'Cintos',
        'Lencos',
        'Chapeus',
        'Oculos artesanais',
        'Capas (celular, notebook)',
        'Bijuterias',
        'Acessorios personalizados'
      ]
    },
    {
      label: 'Moveis',
      value: '6',
      subcategories: [
        'Mesas',
        'Cadeiras',
        'Bancos',
        'Estantes',
        'Prateleiras',
        'Criados-mudos',
        'Moveis rusticos',
        'Moveis planejados',
        'Moveis infantis',
        'Moveis sustentaveis'
      ]
    },
    {
      label: 'Cozinha',
      value: '7',
      subcategories: [
        'Utensilios de madeira',
        'Tabuas de corte',
        'Canecas artesanais',
        'Pratos e Loucas',
        'Copos e Tacas',
        'Panos de prato',
        'Organizadores',
        'Kits de cozinha',
        'Itens personalizados',
        'Decoracao de cozinha'
      ]
    },
    {
      label: 'Papelaria',
      value: '8',
      subcategories: [
        'Cadernos artesanais',
        'Agendas & planners',
        'Blocos de notas',
        'Marcadores de pagina',
        'Cartoes personalizados',
        'Convites (casamento, aniversario, eventos)',
        'Papelaria para casamento',
        'Papelaria infantil',
        'Papelaria escolar',
        'Scrapbook',
        'Adesivos & stickers',
        'Selos & carimbos artesanais',
        'Caixas & embalagens personalizadas',
        'Papelaria corporativa artesanal',
        'Papelaria ecologica'
      ]
    },
    {
      label: 'Outros',
      value: '9',
      subcategories: [
        'Produtos personalizados',
        'Kits presente',
        'Datas comemorativas (Natal, Pascoa, Dia das Maes, etc.)',
        'Lembrancinhas',
        'Produtos sob encomenda',
        'Artesanato regional',
        'Produtos sustentaveis',
        'Itens religiosos',
        'Itens misticos / esotericos',
        'Decoracao sazonal',
        'Produtos exclusivos',
        'Colecionaveis',
        'Itens experimentais / novos produtos'
      ]
    }
  ];

  @Output() toggleDark = new EventEmitter<void>();

  ngOnInit(): void {
    this.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        if (user?.role === 'Seller') {
          this.refreshChatRequests();
        } else {
          this.chatRequestCount = 0;
        }
      });

    this.chatService.notifications$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        const user = this.authService.currentUserValue;
        if (user?.role === 'Seller') {
          this.refreshChatRequests();
        }
      });

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
    this.searchSubject.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      switchMap(term => {
        const trimmed = term.trim();
        if (trimmed.length < 2) {
          this.searchSuggestionsProducts = [];
          this.searchSuggestionsSellers = [];
          this.isSearchLoading = false;
          return of(null);
        }

        this.isSearchLoading = true;
        return forkJoin({
          products: this.productService.getAllProducts(1, 5, trimmed).pipe(
            catchError(() => of({ data: [], items: [] }))
          ),
          sellers: this.sellerService.searchSellers(trimmed, 5).pipe(
            catchError(() => of([]))
          )
        });
      }),
      takeUntil(this.destroy$)
    ).subscribe((res) => {
      if (!res) return;
      const products = Array.isArray(res.products) ? res.products : (res.products.data || res.products.items || []);
      this.searchSuggestionsProducts = products;
      this.searchSuggestionsSellers = Array.isArray(res.sellers) ? res.sellers : [];
      this.isSearchLoading = false;
    });
  }

  ngOnDestroy(): void {
    this.closeMenus();
    this.destroy$.next();
    this.destroy$.complete();
    if (this.routerSub) this.routerSub.unsubscribe();
  }

  onSearch(): void {
    const term = this.searchTerm.trim();
    if (term) {
      this.router.navigate(['/products'], { queryParams: { search: term } });
    } else {
      this.router.navigate(['/']);
    }
    this.isSearchOpen = false;
    this.closeMenus();
  }

  onSearchInput(term: string): void {
    this.isSearchOpen = true;
    this.searchSubject.next(term);
  }

  openSearch(): void {
    this.isSearchOpen = true;
    if (this.searchTerm.trim().length >= 2) {
      this.searchSubject.next(this.searchTerm);
    }
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

    // Fechar menu de usuÃ¡rio se clicar fora
    if (this.isUserMenuOpen && !target.closest('.prevent-close')) {
      this.isUserMenuOpen = false;
    }
    if (this.isSearchOpen && !target.closest('.search-container')) {
      this.isSearchOpen = false;
    }
  }

  closeMenus(): void {
    this.isMobileMenuOpen = false;
    this.isSearchOpen = false;
    this.isSearchLoading = false;
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

  private refreshChatRequests() {
    this.chatService.getContactRequestThreads().subscribe({
      next: threads => {
        this.chatRequestCount = threads.length;
      },
      error: () => {
        this.chatRequestCount = 0;
      }
    });
  }
}









