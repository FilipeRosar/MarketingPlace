import { Component, inject, OnInit, OnDestroy, signal, effect, computed } from '@angular/core'; // Adicionado 'computed'
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject, takeUntil } from 'rxjs';

import { ProductService } from '../../services/product/product.service';
import { ProductCardComponent } from '../../components/product-card/product-card.component';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';
import { SeoService } from '../../services/SEO/seo.service';

interface FilterState {
  search: string;
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
    LoadingSpinnerComponent
  ],
  templateUrl: './categories-page.component.html',
  styleUrl: './categories-page.component.css'
})
export class CategoriesPageComponent implements OnInit, OnDestroy {
  private productService = inject(ProductService);
  private seoService = inject(SeoService); // Injetar SeoService
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  // Dados
  products = signal<any[]>([]);
  filteredProducts = signal<any[]>([]);
  isLoading = signal(true);
  totalItems = signal(0);
  currentPage = signal(1);
  pageSize = 24;

  // Filtros
  filters = signal<FilterState>({
    search: '',
    category: '',
    color: '',
    priceMin: 0,
    priceMax: 2000,
    region: '',
    sortBy: 'relevance'
  });

  // Opções dos filtros (Mapeamento para label)
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
        search: params['search'] || '', // Ajustado para 'search' (era 'q' no exemplo anterior, mas header usa 'search')
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

    // Efeito para atualizar SEO e Filtros quando mudarem
    effect(() => {
      this.applyFilters();

      // Atualiza título da aba do navegador
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
      f.category
    ).subscribe({
      next: (res: any) => {
        const items = Array.isArray(res) ? res : (res.items || res.data || []);
        this.products.set(items);
        this.totalItems.set(res.total || items.length);
        this.applyFilters(); // Reaplica filtros locais se houver (cor, preço)
        this.isLoading.set(false);
      },
      error: () => {
        this.products.set([]);
        this.isLoading.set(false);
      }
    });
  }

  applyFilters() {
    const f = this.filters();
    let filtered = [...this.products()];

    // Filtros locais (que o backend talvez não suporte ainda ou para refinamento)
    if (f.color) {
      // Simulação: filtra se a tag contém a cor
      filtered = filtered.filter(p => p.tags?.some((t: string) => t.toLowerCase().includes(f.color.toLowerCase())));
    }

    if (f.priceMin > 0 || f.priceMax < 2000) {
      filtered = filtered.filter(p => p.price >= f.priceMin && p.price <= f.priceMax);
    }

    // Ordenação local (se o backend não ordenar)
    filtered.sort((a, b) => {
      switch (f.sortBy) {
        case 'price-low': return a.price - b.price;
        case 'price-high': return b.price - a.price;
        // case 'newest': ... (precisa de data no produto)
        default: return 0;
      }
    });

    this.filteredProducts.set(filtered);
  }

  updateUrl(partial: Partial<FilterState>) {
    const current = this.filters();
    // Remove search se categoria mudar, ou vice-versa, se desejar comportamento exclusivo
    // Aqui mantemos acumulativo, mas limpamos search se estiver vazio
    const updated = { ...current, ...partial };

    // Limpeza de objetos vazios para a URL ficar bonita
    const queryParams: any = {
        search: updated.search || null,
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
