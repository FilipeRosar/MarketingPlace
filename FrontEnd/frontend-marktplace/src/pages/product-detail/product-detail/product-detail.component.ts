import { Component, OnInit, inject, Inject, PLATFORM_ID } from '@angular/core';
import { CommonModule, Location, DecimalPipe, isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProductService } from '../../../services/product/product.service';
import { CartService } from '../../../services/cart/cart.service';
import { AuthService } from '../../../services/auth/auth.service';
import { Product } from '../../../models/product/product.model';
import { CurrencyBrPipe } from '../../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../../components/loading-spinner.component/loading-spinner.component';
import { RatingStarsComponent } from '../../../components/rating-stars/rating-stars.component';
import { SeoService } from '../../../services/SEO/seo.service';
import { NotificationService } from '../../../services/notification/notification.service';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyBrPipe, LoadingSpinnerComponent, RatingStarsComponent, ReactiveFormsModule],
  providers: [DecimalPipe],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.css'
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private authService = inject(AuthService);
  private location = inject(Location);
  private fb = inject(FormBuilder);
  private seoService = inject(SeoService);
  private router = inject(Router);
  private notificationService = inject(NotificationService);

  product: Product | null = null;
  isLoading = true;
  error: string | null = null;
  currentUser$ = this.authService.currentUser$;

  reviewForm: FormGroup;
  isSubmittingReview = false;

  mockReviews = [
    { name: 'Maria Souza', date: '22/11/2025', stars: 5, comment: 'Peça linda, superou minhas expectativas! Chegou bem embalado.' },
    { name: 'João Carlos', date: '15/11/2025', stars: 4.5, comment: 'Ótimo trabalho, mas o prazo de entrega foi um pouco longo.' },
  ];

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    this.reviewForm = this.fb.group({
      rating: [0, [Validators.required, Validators.min(1), Validators.max(5)]],
      comment: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]]
    });
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.product = null;
        this.isLoading = true;
        this.error = null;
        this.loadProduct(id);
      }
    });
  }

  loadProduct(id: string) {
    this.isLoading = true;

    this.productService.getProductById(id).subscribe({
      next: (data: any) => { // Usando 'any' temporariamente para tratar a resposta

        if (!data) {
             this.error = 'Produto não encontrado.';
             this.isLoading = false;
             return;
        }

        // --- NORMALIZAÇÃO DE DADOS (Correção de Bugs) ---

        // 1. Garante imageUrl se vier apenas lista de images
        if (!data.imageUrl && data.images && data.images.length > 0) {
            data.imageUrl = typeof data.images[0] === 'string' ? data.images[0] : data.images[0].url;
        }

        // 2. Garante SellerName para não quebrar o charAt(0)
        if (!data.sellerName) {
            data.sellerName = data.seller?.name || 'Vendedor Trama';
        }

        this.product = data;
        this.isLoading = false;

        // Atualiza SEO
        this.seoService.updateSeoData({
          title: this.product?.name || 'Produto',
          description: this.product?.description?.substring(0, 150) || '',
          image: this.product?.imageUrl,
          slug: `/products/${this.product?.id}`
        });

        if (isPlatformBrowser(this.platformId)) {
            window.scrollTo(0, 0);
        }
      },
      error: (err) => {
        console.error('ProductDetail: Erro ao carregar:', err);
        this.error = 'Produto não encontrado ou indisponível.';
        this.isLoading = false;
      }
    });
  }

  addToCart() {
    if (this.product) {
      this.cartService.addToCart(this.product);
    }
  }

  submitReview() {
    if (this.reviewForm.invalid) {
      this.reviewForm.markAllAsTouched();
      return;
    }

    this.isSubmittingReview = true;

    setTimeout(() => {
      this.isSubmittingReview = false;
      this.notificationService.success('Sua avaliação foi enviada com sucesso!');
      this.reviewForm.reset({ rating: 0, comment: '' });
    }, 1500);
  }

  setRating(star: number) {
    if (!this.reviewForm.disabled) {
      this.reviewForm.get('rating')?.setValue(star);
    }
  }

  goBack() {
    this.location.back();
  }
}
