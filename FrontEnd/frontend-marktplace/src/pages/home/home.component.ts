// src/app/pages/home/home.component.ts
import { Component, inject, OnInit, OnDestroy, PLATFORM_ID, ViewChild, ElementRef } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductService } from '../../services/product/product.service';
import { SeoService } from '../../services/SEO/seo.service';
import { Product } from '../../models/product/product.model';
import { ProductCardComponent } from '../../components/product-card/product-card.component';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ProductCardComponent,
    LoadingSpinnerComponent
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit, OnDestroy {
  private productService = inject(ProductService);
  private seoService = inject(SeoService);
  private platformId = inject(PLATFORM_ID);

  @ViewChild('categoriesContainer') categoriesContainer!: ElementRef;

  // Dados
  featuredProducts: Product[] = [];
  isLoading = true;

  // Hero Images (Unsplash estáveis)
  heroImages = [
    { id: 1, src: 'https://images.unsplash.com/photo-1610701596007-11502861dcfa?w=600&q=80' },
    { id: 2, src: 'https://images.unsplash.com/photo-1513519245088-0e12902e5a38?w=600&q=80' },
    { id: 3, src: 'https://images.unsplash.com/photo-1590736969955-71cc94801759?w=600&q=80' },
    { id: 4, src: 'https://images.unsplash.com/photo-1605518216938-7c31b7b14ad0?w=600&q=80' },
    { id: 5, src: 'https://images.unsplash.com/photo-1584589167171-541ce45f1eea?w=600&q=80' },
    { id: 6, src: 'https://images.unsplash.com/photo-1622226069815-843b9f040e96?w=600&q=80' },
    { id: 7, src: 'https://images.unsplash.com/photo-1595079676339-1534801fafde?w=600&q=80' },
    { id: 8, src: 'https://images.unsplash.com/photo-1616627988031-f912e383a694?w=600&q=80' }
  ];

  carouselCategories = [
    { label: 'Decoração', value: '0', img: 'https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=400&q=80' },
    { label: 'Joias', value: '1', img: 'https://images.unsplash.com/photo-1515562141207-7a88fb7ce338?w=400&q=80' },
    { label: 'Roupas', value: '2', img: 'https://images.unsplash.com/photo-1509631179647-0177331693ae?w=400&q=80' },
    { label: 'Arte', value: '3', img: 'https://images.unsplash.com/photo-1579783902614-a3fb3927b6a5?w=400&q=80' },
    { label: 'Móveis', value: '6', img: 'https://images.unsplash.com/photo-1598300042247-d088f8ab3a91?w=400&q=80' },
    { label: 'Cozinha', value: '7', img: 'https://images.unsplash.com/photo-1556910103-1c02745a30bf?w=400&q=80' }
  ];

  private shuffleInterval: any;

  constructor() {
    // CHAMA TUDO ANTES DO RENDER
    this.setupSEO();
    this.loadFeaturedProducts();
  }

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.startShuffle();
    }
  }

  ngOnDestroy(): void {
    if (this.shuffleInterval) clearInterval(this.shuffleInterval);
  }

  private setupSEO(): void {
    this.seoService.updateSeoData({
      title: 'Mitrama - O Maior Marketplace de Artesanato do Brasil',
      description: 'Peças únicas feitas à mão por artesãos de todo o Brasil. Decoração, moda e presentes com alma.',
      image: 'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=1200&q=80',
      slug: '/'
    });
  }

  private loadFeaturedProducts(): void {
    this.isLoading = true;
    this.productService.getAllProducts(1, 8).subscribe({
      next: (data: any) => {
        const items = Array.isArray(data) ? data : (data?.items || data?.data || []);
        this.featuredProducts = items;
        this.isLoading = false;
      },
      error: () => {
        this.featuredProducts = [];
        this.isLoading = false;
      }
    });
  }

  startShuffle(): void {
    this.shuffleSquares();
    this.shuffleInterval = setInterval(() => this.shuffleSquares(), 4000);
  }

  shuffleSquares(): void {
    const array = [...this.heroImages];
    for (let i = array.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [array[i], array[j]] = [array[j], array[i]];
    }
    this.heroImages = array;
  }

  scrollCategories(direction: 'left' | 'right'): void {
    if (isPlatformBrowser(this.platformId) && this.categoriesContainer) {
      const amount = 300;
      this.categoriesContainer.nativeElement.scrollBy({
        left: direction === 'left' ? -amount : amount,
        behavior: 'smooth'
      });
    }
  }
}
