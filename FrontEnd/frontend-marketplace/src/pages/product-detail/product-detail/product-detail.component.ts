import { Component, OnInit, inject, Inject, PLATFORM_ID } from '@angular/core';
import { CommonModule, Location, DecimalPipe, isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
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
import { RatingsService, RatingDto } from '../../../services/ratings/ratings.service';

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
  private ratingsService = inject(RatingsService);
  private location = inject(Location);
  private fb = inject(FormBuilder);
  private seoService = inject(SeoService);
  private notificationService = inject(NotificationService);

  product: Product | null = null;
  isLoading = true;
  error: string | null = null;
  currentUser$ = this.authService.currentUser$;

  // Gallery
  galleryImages: string[] = [];
  currentImageIndex = 0;

  // Zoom
  isZooming = false;
  zoomPosition = { x: 0, y: 0 };

  reviewForm: FormGroup;
  isSubmittingReview = false;
  replyDrafts: Record<string, string> = {};
  replySubmitting: Record<string, boolean> = {};

  reviews: RatingDto[] = [];
  activeTab: 'details' | 'story' = 'details';
  storyHtml = '';
  displayAverageRating = 0;
  displayTotalRatings = 0;
  ratingsPage = 1;
  ratingsPageSize = 10;
  ratingsPages = 0;
  isLoadingReviews = false;

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    this.reviewForm = this.fb.group({
      rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
      comment: ['', [Validators.required, Validators.maxLength(500)]]
    });
  }

  ngOnInit() {
  this.route.paramMap.subscribe(params => {
    const id = params.get('id');
    if (id) this.loadProduct(id);
  });

  this.currentUser$.subscribe(user => {
    console.log('Usuário atual:', user);
    if (user) {
      console.log('Role do usuário:', user.role);
      console.log('É Customer?', user.role === 'Customer');
    } else {
      console.log('Usuário não logado');
    }
  });
}
  loadProduct(id: string) {
    this.isLoading = true;
    this.productService.getProductById(id).subscribe({
      next: (data: any) => {
        if (!data) {
          this.error = 'Produto nao encontrado.';
          this.isLoading = false;
          return;
        }

        this.galleryImages = [];

        if (data.images && Array.isArray(data.images) && data.images.length > 0) {
          this.galleryImages = data.images.map((img: any) =>
            typeof img === 'string' ? img : img.url
          );
        } else if (data.imageUrl) {
          this.galleryImages = [data.imageUrl];
        }

        if (!data.sellerName) {
          data.sellerName = data.seller?.name || 'Vendedor Trama';
        }

        this.product = data;
        this.activeTab = 'details';
        this.storyHtml = this.renderMarkdown(this.product?.storyMarkdown || '');
        this.isLoading = false;
        this.loadReviews(1);

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
      error: () => {
        this.error = 'Produto indisponivel.';
        this.isLoading = false;
      }
    });
  }

  private loadReviews(page: number) {
    if (!this.product) return;

    this.isLoadingReviews = true;
    this.ratingsService.getByProduct(this.product.id, page, this.ratingsPageSize).subscribe({
      next: (res) => {
        this.reviews = res.data ?? [];
        this.displayAverageRating = res.averageRating ?? 0;
        this.displayTotalRatings = res.total ?? 0;
        this.ratingsPage = res.page ?? page;
        this.ratingsPages = res.pages ?? 0;
        this.isLoadingReviews = false;
      },
      error: () => {
        this.reviews = [];
        this.displayAverageRating = this.product?.averageRating ?? 0;
        this.displayTotalRatings = this.product?.totalRatings ?? 0;
        this.isLoadingReviews = false;
      }
    });
  }

  // Gallery controls
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
  if (this.isSubmittingReview || this.reviewForm.invalid || !this.product) {
    return;
  }
  console.log('Form status:', this.reviewForm.status);
  console.log('Form value:', this.reviewForm.value);
  console.log('Form errors:', this.reviewForm.errors);
  console.log('Comment control errors:', this.reviewForm.get('comment')?.errors);
  const rating = Number(this.reviewForm.get('rating')?.value);
  const comment = String(this.reviewForm.get('comment')?.value || '').trim();

  console.log('Enviando avaliação:', { productId: this.product.id, rating, comment });

  this.isSubmittingReview = true;

  this.ratingsService.createRating(this.product.id, rating, comment).subscribe({
    next: (res) => {
      console.log('Avaliação enviada com sucesso:', res);
      this.notificationService.success('Avaliação enviada!');
      this.reviewForm.reset({ rating: 5, comment: '' });
      this.loadReviews(1);
    },
    error: (err) => {
      console.error('Erro completo ao enviar avaliação:', err);
      const msg = err.error?.message || err.message || 'Erro desconhecido.';
      this.notificationService.error(`Falha ao enviar: ${msg}`);
    },
    complete: () => {
      this.isSubmittingReview = false;
    }
  });
}

  onReplyInput(ratingId: string, event: Event) {
    const value = (event.target as HTMLTextAreaElement).value;
    this.replyDrafts[ratingId] = value;
  }

  submitSellerReply(review: RatingDto) {
    const draft = this.replyDrafts[review.id];
    const reply = String(draft ?? review.sellerReply ?? '').trim();

    if (!reply) {
      this.notificationService.warning('Escreva uma resposta antes de enviar.');
      return;
    }

    if (this.replySubmitting[review.id]) {
      return;
    }

    this.replySubmitting[review.id] = true;
    this.ratingsService.replyToRating(review.id, reply).subscribe({
      next: () => {
        this.notificationService.success('Resposta enviada com sucesso!');
        this.replyDrafts[review.id] = '';
        this.replySubmitting[review.id] = false;
        this.loadReviews(this.ratingsPage);
      },
      error: (err) => {
        const msg = err?.error?.message || 'Erro ao enviar resposta.';
        this.notificationService.error(msg);
        this.replySubmitting[review.id] = false;
      }
    });
  }

  setRating(star: number) {
    if (!this.reviewForm.disabled) {
      this.reviewForm.get('rating')?.setValue(star);
    }
  }

  private getDisplayPrice(): number {
    if (!this.product) return 0;
    if (this.product.salePrice && this.product.salePrice > 0 && this.product.salePrice < this.product.price) {
      return this.product.salePrice;
    }
    return this.product.price;
  }

  getMaxInstallments(): number {
    if (!this.product) return 1;
    const max = this.product.maxInstallments ?? 12;
    return Math.min(12, Math.max(1, Math.floor(max)));
  }

  getNoInterestInstallments(): number {
    if (!this.product) return 0;
    const max = this.getMaxInstallments();
    const noInterest = this.product.maxNoInterestInstallments ?? 0;
    return Math.min(max, Math.max(0, Math.floor(noInterest)));
  }

  getInstallmentValue(): number {
    const price = this.getDisplayPrice();
    const max = this.getMaxInstallments();
    return Number((price / max).toFixed(2));
  }


  setTab(tab: 'details' | 'story') {
    this.activeTab = tab;
  }

  private renderMarkdown(text: string): string {
    if (!text) return '';

    const escaped = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');

    const lines = escaped.split(/\r?\n/);
    const html: string[] = [];
    let inList = false;

    const applyInline = (line: string) => line
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.+?)\*/g, '<em>$1</em>')
      .replace(/\[(.+?)\]\((https?:\/\/[^\s]+)\)/g, '<a href="$2" target="_blank" rel="noopener" class="text-primary underline">$1</a>');

    for (const rawLine of lines) {
      const line = rawLine.trimRight();

      if (/^[-*]\s+/.test(line)) {
        if (!inList) {
          html.push('<ul class="list-disc pl-5 space-y-1">');
          inList = true;
        }
        html.push(`<li>${applyInline(line.replace(/^[-*]\s+/, ''))}</li>`);
        continue;
      }

      if (inList) {
        html.push('</ul>');
        inList = false;
      }

      if (line.startsWith('### ' )) {
        html.push(`<h4 class="text-base font-semibold text-gray-900 mt-4">${applyInline(line.slice(4))}</h4>`);
        continue;
      }
      if (line.startsWith('## ' )) {
        html.push(`<h3 class="text-lg font-semibold text-gray-900 mt-4">${applyInline(line.slice(3))}</h3>`);
        continue;
      }
      if (line.startsWith('# ' )) {
        html.push(`<h2 class="text-xl font-semibold text-gray-900 mt-4">${applyInline(line.slice(2))}</h2>`);
        continue;
      }

      if (!line.trim()) {
        html.push('<div class="h-3"></div>');
        continue;
      }

      html.push(`<p class="text-sm text-gray-700 leading-relaxed">${applyInline(line)}</p>`);
    }

    if (inList) {
      html.push('</ul>');
    }

    return html.join('');
  }
  goBack() {
    this.location.back();
  }
}
