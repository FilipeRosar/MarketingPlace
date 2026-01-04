import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SellerService } from '../../../services/seller/seller.service';
import { ProductService } from '../../../services/product/product.service';
import { AuthService } from '../../../services/auth/auth.service';
import { UserService } from '../../../services/user/user.service';
import { Product } from '../../../models/product/product.model';
import { ProductCardComponent } from '../../../components/product-card/product-card.component';
import { LoadingSpinnerComponent } from '../../../components/loading-spinner.component/loading-spinner.component';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
  selector: 'app-seller-profile',
  standalone: true,
  imports: [CommonModule, RouterLink, ProductCardComponent, LoadingSpinnerComponent, FormsModule],
  templateUrl: './seller-profile.html',
  styleUrl: './seller-profile.css'
})
export class SellerProfileComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private sellerService = inject(SellerService);
  private productService = inject(ProductService);
  private authService = inject(AuthService);
  private sanitizer = inject(DomSanitizer);
  private userService = inject(UserService);
  private location = inject(Location);

  seller: any | null = null;
  products: Product[] = [];
  isLoading = true;
  isOwner = false;

  // Estado de Edição
  isEditingBio = false;
  editData = { name: '', bio: '', instagram: '', facebook: '', tiktok: '', youtube: '' };
  activeTab: 'products' | 'about' | 'reviews' | 'moments' = 'products';

  moments = [
    { id: '1', videoUrl: 'assets/videos/artisan-working.mp4', thumb: 'assets/images/moment1-thumb.jpg', desc: 'Finalizando uma peça sob medida ✨' },
    { id: '2', videoUrl: 'assets/videos/studio-tour.mp4', thumb: 'assets/images/moment2-thumb.jpg', desc: 'Um pouco do nosso cantinho criativo 🏡' }
  ];
  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const sellerId = params.get('id');
      if (sellerId) {
        this.checkOwnership(sellerId);
        this.loadSellerData(sellerId);
      }
    });
  }

  checkOwnership(profileId: string) {
    const currentUser = this.authService.currentUserValue;
    // Compara ID do perfil com ID do usuário logado
    if (currentUser && currentUser.id === profileId) {
      this.isOwner = true;
    }
  }

  loadSellerData(id: string) {
    this.isLoading = true;
    this.sellerService.getSellerById(id).subscribe({
      next: (data) => {
        this.seller = data;
        this.loadSellerProducts(id);
      },
      error: (err) => {
        console.error('Erro ao carregar vendedor', err);
        this.isLoading = false;
      }
    });
  }

  loadSellerProducts(sellerId: string) {
    // Filtra produtos deste vendedor
        this.productService.getAllProducts(1, 100, '', '', undefined, undefined, sellerId).subscribe({
        next: (response: any) => {
        const all = Array.isArray(response) ? response : (response.data || response.items || []);
        this.products = all.filter((p: any) => p.sellerId === sellerId);
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  // --- EDIÇÃO DE PERFIL (BIO) ---

  startEditBio() {
    this.editData = {
      name: this.seller.name,
      bio: this.seller.bio || '',
      instagram: this.seller.instagram || '',
      facebook: this.seller.facebook || '',
      tiktok: this.seller.tiktok || '',
      youtube: this.seller.youtube || ''
    };
    this.isEditingBio = true;
  }

  saveBio() {
    if (!this.seller) return;

    this.seller.name = this.editData.name;
    this.seller.bio = this.editData.bio;
    this.seller.instagram = this.editData.instagram;
    this.seller.facebook = this.editData.facebook;
    this.seller.tiktok = this.editData.tiktok;
    this.seller.youtube = this.editData.youtube;

    this.isEditingBio = false;

    this.sellerService.updateProfile({
      ...this.seller,
    }).subscribe({
      next: () => alert('Perfil atualizado!'),
      error: (err) => {
        console.error(err);
        alert('Erro ao salvar perfil.');
      }
    });
  }
  // --- UPLOAD DE IMAGENS ---

  onAvatarSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
        this.userService.uploadProfilePhoto(file).subscribe({
            next: (res) => {
                this.seller.profileImageUrl = res.imageUrl;
                // Se for o dono, atualiza o header também
                if (this.isOwner) {
                   const currentUser = this.authService.currentUserValue;
                   if (currentUser) {
                       this.authService.updateCurrentUser({ ...currentUser, profileImageUrl: res.imageUrl });
                   }
                }
            },
            error: () => alert('Erro ao enviar logo.')
        });
    }
  }

  onBannerSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
        // Usa o método específico para banner no SellerService
        this.sellerService.uploadBanner(file).subscribe({
            next: (res) => {
                this.seller.bannerImageUrl = res.imageUrl;
            },
            error: () => alert('Erro ao enviar capa.')
        });
    }
  }
  setActiveTab(tab: 'products' | 'about' | 'reviews' | 'moments') {
    this.activeTab = tab;
  }
  goBack() {
    this.location.back();
  }
}
