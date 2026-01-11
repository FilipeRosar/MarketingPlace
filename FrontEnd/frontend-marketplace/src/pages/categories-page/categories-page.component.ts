import { Component, inject, OnInit, OnDestroy, signal, effect, computed } from '@angular/core'; // Adicionado 'computed'
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject, takeUntil } from 'rxjs';

import { ProductService } from '../../services/product/product.service';
import { SellerService } from '../../services/seller/seller.service';
import { ProductCardComponent } from '../../components/product-card/product-card.component';
import { SellerCardComponent } from '../../components/seller-card/seller-card/seller-card.component';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';
import { SeoService } from '../../services/SEO/seo.service';

interface FilterState {
  search: string;
  subcategory: string;
  category: string;
  color: string;
  priceMin: number;
  priceMax: number;
  region: string;
  sortBy: string;
}

@Component({
  selector: 'app-categories-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    ProductCardComponent,
    SellerCardComponent,
    LoadingSpinnerComponent
  ],
  templateUrl: './categories-page.component.html',
  styleUrl: './categories-page.component.css'
})
export class CategoriesPageComponent implements OnInit, OnDestroy {
  private productService = inject(ProductService);
  private sellerService = inject(SellerService);
  private seoService = inject(SeoService); // Injetar SeoService
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  // Dados
  products = signal<any[]>([]);
  sellers = signal<any[]>([]);
  filteredProducts = signal<any[]>([]);
  isLoading = signal(true);
  isSellerLoading = signal(false);
  totalItems = signal(0);
  currentPage = signal(1);
  pageSize = 24;

  // Filtros
  filters = signal<FilterState>({
    search: '',
    subcategory: '',
    category: '',
    color: '',
    priceMin: 0,
    priceMax: 2000,
    region: '',
    sortBy: 'relevance'
  });

  categories = [
    { value: '', label: 'Todas as categorias' },
    { value: '0', label: 'Decoração' },
    { value: '1', label: 'Joias' },
    { value: '2', label: 'Roupas' },
    { value: '3', label: 'Arte' },
    { value: '4', label: 'Brinquedos' },
    { value: '5', label: 'Acessórios' },
    { value: '6', label: 'Móveis' },
    { value: '7', label: 'Cozinha' },
    { value: '8', label: 'Papelaria' },
    { value: '9', label: 'Outros' }
  ];

  subcategoriesByCategory: Record<string, string[]> = {
    '0': [
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
    ],
    '1': [
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
    ],
    '2': [
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
    ],
    '3': [
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
    ],
    '4': [
      'Brinquedos educativos',
      'Brinquedos de madeira',
      'Bonecas artesanais',
      'Amigurumi',
      'Jogos pedagogicos',
      'Quebra-cabecas',
      'Brinquedos sensoriais',
      'Brinquedos infantis',
      'Brinquedos personalizados'
    ],
    '5': [
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
    ],
    '6': [
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
    ],
    '7': [
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
    ],
    '8': [
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
    ],
    '9': [
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
  };

  subcategoryOptions = computed(() => {
    const category = this.filters().category;
    return this.subcategoriesByCategory[category] || [];
  });

  colors = [
    { name: 'Terracota', hex: '#b45309' },
    { name: 'Sálvia', hex: '#84a98c' },
    { name: 'Areia', hex: '#d6ccc2' },
    { name: 'Carvão', hex: '#264653' },
    { name: 'Mostarda', hex: '#e9c46a' },
    { name: 'Lavanda', hex: '#cdb4db' }
  ];

  regions = ['Todas as regiões', 'Nordeste', 'Sudeste', 'Sul', 'Norte', 'Centro-Oeste'];
  sortOptions = [
    { value: 'relevance', label: 'Mais relevantes' },
    { value: 'newest', label: 'Mais recentes' },
    { value: 'price-low', label: 'Menor preço' },
    { value: 'price-high', label: 'Maior preço' },
    { value: 'bestsellers', label: 'Mais vendidos' }
  ];

  // Título Dinâmico (Computed Signal)
  pageTitle = computed(() => {
    const f = this.filters();

    if (f.search) {
      return `Resultados para "${f.search}"`;
    }

    if (f.subcategory) {
      if (f.category) {
        const cat = this.categories.find(c => c.value === f.category);
        return cat ? `${cat.label} - ${f.subcategory}` : f.subcategory;
      }
      return `Subcategoria: ${f.subcategory}`;
    }

    if (f.category) {
      const cat = this.categories.find(c => c.value === f.category);
      return cat ? cat.label : 'Categoria';
    }

    if (f.color) {
      return `Produtos na cor ${f.color}`;
    }

    return 'Explorar Produtos';
  });

  // Debounce na busca
  private searchSubject = new Subject<string>();

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.filters.update(f => ({
        ...f,
        search: params['search'] || '',
        subcategory: params['subcategory'] || '',
        category: params['category'] || '',
        color: params['color'] || '',
        region: params['region'] || '',
        sortBy: params['sort'] || 'relevance'
      }));
      this.currentPage.set(1);
      this.loadProducts();
    });

    this.searchSubject.pipe(
      debounceTime(500),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(term => {
      this.updateUrl({ search: term });
    });

    effect(() => {
      this.applyFilters();


      this.seoService.updateSeoData({
        title: this.pageTitle(),
        description: `Encontre o melhor de ${this.pageTitle()} no Trama.`,
        slug: this.router.url
      });
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadProducts() {
    this.isLoading.set(true);
    const f = this.filters();

    this.productService.getAllProducts(
      this.currentPage(),
      this.pageSize,
      f.search,
      f.category,
      f.subcategory
    ).subscribe({
      next: (res: any) => {
        const items = Array.isArray(res) ? res : (res.items || res.data || []);
        this.products.set(items);
        this.totalItems.set(res.total || items.length);
        this.applyFilters();
        this.isLoading.set(false);
      },
      error: () => {
        this.products.set([]);
        this.isLoading.set(false);
      }
    });

    if (f.search && f.search.trim().length >= 2) {
      this.loadSellers(f.search.trim());
    } else {
      this.sellers.set([]);
    }
  }

  private loadSellers(term: string) {
    this.isSellerLoading.set(true);
    this.sellerService.searchSellers(term, 8).subscribe({
      next: (data) => {
        this.sellers.set(Array.isArray(data) ? data : []);
        this.isSellerLoading.set(false);
      },
      error: () => {
        this.sellers.set([]);
        this.isSellerLoading.set(false);
      }
    });
  }

  applyFilters() {
    const f = this.filters();
    let filtered = [...this.products()];

    if (f.color) {
      filtered = filtered.filter(p => p.tags?.some((t: string) => t.toLowerCase().includes(f.color.toLowerCase())));
    }

    if (f.priceMin > 0 || f.priceMax < 2000) {
      filtered = filtered.filter(p => p.price >= f.priceMin && p.price <= f.priceMax);
    }

    filtered.sort((a, b) => {
      switch (f.sortBy) {
        case 'price-low': return a.price - b.price;
        case 'price-high': return b.price - a.price;
        default: return 0;
      }
    });

    this.filteredProducts.set(filtered);
  }

  updateUrl(partial: Partial<FilterState>) {
    const current = this.filters();
    const updated = { ...current, ...partial };

    const queryParams: any = {
        search: updated.search || null,
        subcategory: updated.subcategory || null,
        category: updated.category || null,
        color: updated.color || null,
        region: updated.region || null,
        sort: updated.sortBy !== 'relevance' ? updated.sortBy : null
    };

    this.router.navigate([], {
      queryParams,
      queryParamsHandling: 'merge',
    });
  }

  onSearchChange(term: string) {
    this.searchSubject.next(term);
  }

  clearFilters() {
    this.router.navigate(['/categorias']);
  }

  changePage(page: number) {
    this.currentPage.set(page);
    this.loadProducts();
    window.scrollTo(0, 0);
  }
}


