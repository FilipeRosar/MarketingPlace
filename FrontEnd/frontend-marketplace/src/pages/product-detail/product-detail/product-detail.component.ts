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
  private notificationService = inject(NotificationService);

  product: Product | null = null;
  isLoading = true;
  error: string | null = null;
  currentUser$ = this.authService.currentUser$;

  // --- NOVAS PROPRIEDADES DA GALERIA ---
  galleryImages: string[] = [];
  currentImageIndex = 0;

  // Controle de Zoom
  isZooming = false;
  zoomPosition = { x: 0, y: 0 };

  reviewForm: FormGroup;
  isSubmittingReview = false;

  mockReviews = [
    { name: 'Maria Souza', date: '22/11/2025', stars: 5, comment: 'Peça linda, superou minhas expectativas!' },
    { name: 'João Carlos', date: '15/11/2025', stars: 4.5, comment: 'Ótimo trabalho, mas a entrega demorou.' },
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
        this.loadProduct(id);
      }
    });
  }

  loadProduct(id: string) {
    this.isLoading = true;
    this.productService.getProductById(id).subscribe({
      next: (data: any) => {
        if (!data) {
             this.error = 'Produto não encontrado.';
             this.isLoading = false;
             return;
        }

        this.galleryImages = [];

        if (data.images && Array.isArray(data.images) && data.images.length > 0) {
           this.galleryImages = data.images.map((img: any) =>
             typeof img === 'string' ? img : img.url
           );
        }
        else if (data.imageUrl) {
           this.galleryImages = [data.imageUrl];
        }

        if (!data.sellerName) {
            data.sellerName = data.seller?.name || 'Vendedor Trama';
        }

        this.product = data;
        this.isLoading = false;

        // Atualiza SEO com a imagem principal (índice 0)
        this.seoService.updateSeoData({
          title: this.product?.name || 'Produto',
          description: this.product?.description?.substring(0, 150) || '',
          image: this.galleryImages[0],
          slug: `/products/${this.product?.id}`
        });

        if (isPlatformBrowser(this.platformId)) {
           window.scrollTo(0, 0);
        }
      },
      error: (err) => {
        this.error = 'Produto indisponível.';
        this.isLoading = false;
      }
    });
  }

  // --- CONTROLE DA GALERIA ---
  selectImage(index: number) {
    this.currentImageIndex = index;
  }

  onMouseMove(e: MouseEvent) {
    const imageElement = e.target as HTMLElement;
    const rect = imageElement.getBoundingClientRect();

    const x = ((e.clientX - rect.left) / rect.width) * 100;
    const y = ((e.clientY - rect.top) / rect.height) * 100;

    this.zoomPosition = { x, y };
    this.isZooming = true;
  }

  onMouseLeave() {
    this.isZooming = false;
    setTimeout(() => {
        if (!this.isZooming) this.zoomPosition = { x: 50, y: 50 };
    }, 200);
  }

  addToCart() {
    if (this.product) {
      const productToAdd = { ...this.product, imageUrl: this.galleryImages[this.currentImageIndex] };
      this.cartService.addToCart(productToAdd);
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
