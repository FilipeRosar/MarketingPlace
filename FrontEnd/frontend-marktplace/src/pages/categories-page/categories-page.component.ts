// src/app/pages/categories/categories-page.component.ts
import { Component, inject, OnInit, OnDestroy, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject, takeUntil } from 'rxjs';

import { ProductService } from '../../services/product/product.service';
import { ProductCardComponent } from '../../components/product-card/product-card.component';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';

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

  // Opções dos filtros
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

  // Debounce na busca
  private searchSubject = new Subject<string>();

  ngOnInit() {
    // Ler query params na entrada
    this.route.queryParams.subscribe(params => {
      this.filters.update(f => ({
        ...f,
        search: params['q'] || '',
        category: params['category'] || '',
        color: params['color'] || '',
        region: params['region'] || '',
        sortBy: params['sort'] || 'relevance'
      }));
      this.currentPage.set(1);
      this.loadProducts();
    });

    // Debounce na busca digitada
    this.searchSubject.pipe(
      debounceTime(500),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(term => {
      this.updateUrl({ search: term });
    });

    // Reagir a mudanças nos filtros
    effect(() => {
      this.applyFilters();
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Carregar produtos
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
        this.isLoading.set(false);
      },
      error: () => {
        this.products.set([]);
        this.isLoading.set(false);
      }
    });
  }

  // Aplicar filtros locais (cor, preço, região)
  applyFilters() {
    const f = this.filters();
    let filtered = [...this.products()];

    if (f.color) {
      filtered = filtered.filter(p => p.tags?.toLowerCase().includes(f.color.toLowerCase()));
    }
    if (f.priceMin > 0 || f.priceMax < 2000) {
      filtered = filtered.filter(p => p.price >= f.priceMin && p.price <= f.priceMax);
    }
    if (f.region && f.region !== 'Todas as regiões') {
      filtered = filtered.filter(p => p.seller?.region === f.region);
    }

    // Ordenação
    filtered.sort((a, b) => {
      switch (f.sortBy) {
        case 'price-low': return a.price - b.price;
        case 'price-high': return b.price - a.price;
        case 'newest': return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
        default: return 0;
      }
    });

    this.filteredProducts.set(filtered);
  }

  // Atualizar URL sem recarregar
  updateUrl(partial: Partial<FilterState>) {
    const current = this.filters();
    const updated = { ...current, ...partial, page: undefined };
    this.router.navigate([], {
      queryParams: updated,
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  // Handlers
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
