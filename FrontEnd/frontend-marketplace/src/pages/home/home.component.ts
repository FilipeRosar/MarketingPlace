// src/app/pages/home/home.component.ts
import { Component, inject, OnInit, OnDestroy, PLATFORM_ID, ViewChild, ElementRef, NgZone } from '@angular/core';
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
  private ngZone = inject(NgZone);

  @ViewChild('categoriesContainer') categoriesContainer!: ElementRef;

  // Dados
  featuredProducts: Product[] = [];
  isLoading = true;

  heroImages = [
    { id: 1, src: 'src/assets/img/photo-1513519245088-0e12902e5a38.jpeg' },
    { id: 2, src: 'src/assets/img/photo-1584589167171-541ce45f1eea.jpeg' },
    { id: 3, src: 'src/assets/img/photo-1590736969955-71cc94801759.jpeg' },
    { id: 4, src: 'src/assets/img/photo-1605518216938-7c31b7b14ad0.jpeg' },
    { id: 5, src: 'src/assets/img/photo-1610701596007-11502861dcfa.jpeg' },
    { id: 6, src: 'src/assets/img/debby-hudson-MzSqFPLo8CE-unsplash.jpeg' },
    { id: 7, src: 'src/assets/img/dewang-gupta-ESEnXckWlLY-unsplash.jpeg' },
    { id: 8, src: 'src/assets/img/henrik-donnestad-t2Sai-AqIpI-unsplash.jpeg' }
    ];

  carouselCategories = [
    { label: 'Decoração', value: '0', img: 'https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=400&q=80' },
    { label: 'Joias', value: '1', img: 'https://images.unsplash.com/photo-1515562141207-7a88fb7ce338?w=400&q=80' },
    { label: 'Roupas', value: '2', img: 'https://images.unsplash.com/photo-1509631179647-0177331693ae?w=400&q=80' },
    { label: 'Arte', value: '3', img: 'https://images.unsplash.com/photo-1579783902614-a3fb3927b6a5?w=400&q=80' },
    { label: 'Móveis', value: '6', img: 'https://images.unsplash.com/photo-1598300042247-d088f8ab3a91?w=400&q=80' },
    { label: 'Cozinha', value: '7', img: '/assets/img/kitchen.jpg' }
  ];

  private shuffleInterval: any;

  constructor() {
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
    this.ngZone.runOutsideAngular(() => {
      this.shuffleInterval = setInterval(() => {
        this.ngZone.run(() => this.shuffleSquares());
      }, 4000);
    });
  }

  shuffleSquares(): void {
  const index = Math.floor(Math.random() * this.heroImages.length);
  const temp = this.heroImages[0];
  this.heroImages[0] = this.heroImages[index];
  this.heroImages[index] = temp;
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
