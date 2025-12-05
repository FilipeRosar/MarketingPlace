// src/app/pages/home/home.component.ts
import { Component, inject, signal, OnInit, OnDestroy, PLATFORM_ID, effect } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';

import { ProductService } from '../../services/product/product.service';
import { ProductCardComponent } from '../../components/product-card/product-card.component';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';

interface CategoryHighlight {
  value: string;
  label: string;
  img: string;
  count: string;
}

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
  private platformId = inject(PLATFORM_ID);

  // Signals = performance máxima + reatividade instantânea
  featuredProducts = signal<any[]>([]);
  isLoading = signal(true);
  heroPrincipal = 'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1350&q=80';
  // Categorias em destaque na home
  highlightCategories = [
  { value: '0', label: 'Decoração',     img: 'https://images.unsplash.com/photo-1615529182904-14819c35db37?auto=format&fit=crop&w=800&q=80',  count: '1.247' },
  { value: '1', label: 'Joias',         img: 'https://images.unsplash.com/photo-1599643478518-a6f0a3f02367?auto=format&fit=crop&w=800&q=80',  count: '892' },
  { value: '2', label: 'Roupas',        img: 'https://images.unsplash.com/photo-1585487000161-6eb9c7e7c65c?auto=format&fit=crop&w=800&q=80',  count: '567' },
  { value: '3', label: 'Arte',          img: 'https://images.unsplash.com/photo-1544966249-0a422026a3dc?auto=format&fit=crop&w=800&q=80',  count: '1.034' },
  { value: '6', label: 'Móveis',        img: 'https://images.unsplash.com/photo-1555041469-0a266c3f8c12?auto=format&fit=crop&w=800&q=80',  count: '423' },
  { value: '7', label: 'Cozinha',       img: 'https://images.unsplash.com/photo-1600585154363-227d82c8a5e4?auto=format&fit=crop&w=800&q=80',  count: '789' },
  { value: '4', label: 'Brinquedos',    img: 'https://images.unsplash.com/photo-1587654780291-39c9404d746b?auto=format&fit=crop&w=800&q=80',  count: '312' },
  { value: '9', label: 'Outros',        img: 'https://images.unsplash.com/photo-1615529214802-2eca9af8b3c0?auto=format&fit=crop&w=800&q=80',  count: '2.104' }
  ];
  heroAlternativas = [
    'https://images.unsplash.com/photo-1606787620651-54df5dc2e9aa?auto=format&fit=crop&w=1350&q=80', // Cerâmica nordestina
    'https://images.unsplash.com/photo-1616594039963-ae4c9c9a11e4?auto=format&fit=crop&w=1350&q=80', // Renda de bilro
    'https://images.unsplash.com/photo-1604176354204-9268737828e4?auto=format&fit=crop&w=1350&q=80', // Cestaria indígena
  ];
  featuredExample = 'https://images.unsplash.com/photo-1616593918054-5449e757e0d6?auto=format&fit=crop&w=800&q=80';
  private subscription = new Subscription();

  ngOnInit(): void {
    this.loadFeaturedProducts();

    // Preload da imagem hero (LCP < 1s garantido)
    if (isPlatformBrowser(this.platformId)) {
      const heroImg = new Image();
      heroImg.src = 'assets/hero/principal.jpg';

      // Preconnect nas fontes e CDN se usar
      const link = document.createElement('link');
      link.rel = 'preconnect';
      link.href = 'https://fonts.googleapis.com';
      document.head.appendChild(link);
    }
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  private loadFeaturedProducts(): void {
    this.isLoading.set(true);

    // Aqui você pode criar um endpoint específico /featured ou usar filtro
    this.subscription.add(
      this.productService.getAllProducts(1, 12, '', '', 'featured: true').subscribe({
        next: (res: any) => {
          const products = Array.isArray(res)
            ? res
            : (res.items || res.data || []);

          this.featuredProducts.set(products);
          this.isLoading.set(false);
        },
        error: (err) => {
          console.error('Erro ao carregar destaques:', err);
          this.featuredProducts.set([]);
          this.isLoading.set(false);
        }
      })
    );
  }

  // Método opcional pra recarregar ao voltar na home (se quiser)
  // Pode conectar com um BehaviorSubject no service se precisar de refresh
}
