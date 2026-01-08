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
import { MomentResponseDto } from '../../../models/moment/moment.model';
import { NotificationService } from '../../../services/notification/notification.service';

@Component({
  selector: 'app-seller-profile',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ProductCardComponent,
    LoadingSpinnerComponent,
    FormsModule
  ],
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
  private notification = inject(NotificationService);

  seller: any | null = null;
  products: Product[] = [];
  isLoading = true;
  isOwner = false;
  isAddingMoment = false;
  newMoment = {
    videoFile: null as File | null,
    thumbFile: null as File | null,
    description: ''
  };
  isUploadingMoment = false;
  // Estado de Edição
  isEditingBio = false;
  editData = { name: '', bio: '', instagram: '', facebook: '', tiktok: '', youtube: '' };
  activeTab: 'products' | 'about' | 'reviews' | 'moments' = 'products';

  moments: MomentResponseDto[] = [];

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.checkOwnership(id);
        this.loadSellerData(id);
      }
    });
  }

  checkOwnership(profileId: string) {
    const currentUser = this.authService.currentUserValue;
    if (currentUser && currentUser.id === profileId) {
      this.isOwner = true;
    }
  }

  loadSellerData(id: string) {
    this.isLoading = true;

    const isByUserRoute = this.route.snapshot.routeConfig?.path === 'seller-profile/by-user/:id';

    const request$ = isByUserRoute
      ? this.sellerService.getSellerByUserId(id)   // Busca por UserId
      : this.sellerService.getSellerById(id);      // Busca por Seller.Id

    request$.subscribe({
      next: (data) => {
        this.seller = data;
        this.loadMoments(data.id);
        this.loadSellerProducts(data.id);
      },
      error: (err) => {
        console.error('Erro ao carregar vendedor', err);
        this.isLoading = false;
      }
    });
  }
 private loadMoments(sellerId: string) {
  this.sellerService.getMoments(sellerId).subscribe({
    next: (moments) => {
      this.moments = moments;
      this.isLoading = false;
    },
    error: (err) => {
      console.error('Erro ao carregar momentos', err);
      this.moments = [];
      this.isLoading = false;
    }
  });
}
  loadSellerProducts(sellerId: string) {
    this.productService.getAllProducts(1, 100, '', '', undefined, undefined, undefined, sellerId).subscribe({
      next: (response: any) => {
        const all = Array.isArray(response) ? response : (response.data || response.items || []);
        this.products = all.filter((p: any) => p.sellerId === sellerId);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  // --- EDIÇÃO DE PERFIL (BIO) ---
  startEditBio() {
    if (!this.seller) return;

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

    const updatedSeller = {
      ...this.seller,
      name: this.editData.name,
      bio: this.editData.bio,
      instagram: this.editData.instagram,
      facebook: this.editData.facebook,
      tiktok: this.editData.tiktok,
      youtube: this.editData.youtube
    };

    this.sellerService.updateProfile(updatedSeller).subscribe({
      next: () => {
        // Atualiza localmente
        this.seller.name = this.editData.name;
        this.seller.bio = this.editData.bio;
        this.seller.instagram = this.editData.instagram;
        this.seller.facebook = this.editData.facebook;
        this.seller.tiktok = this.editData.tiktok;
        this.seller.youtube = this.editData.youtube;

        alert('Perfil atualizado com sucesso!');
        this.isEditingBio = false;
      },
      error: (err) => {
        console.error('Erro ao salvar perfil', err);
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
        // Atualiza localmente para feedback imediato
        if (this.seller) {
          this.seller.profileImageUrl = res.imageUrl;
        }
        // Opcional: recarrega o seller da API para garantir sincronia
        // this.loadSellerData(this.seller.id);
      },
      error: () => this.notification.error('Erro ao enviar foto.')
    });
  }
}

onBannerSelected(event: any) {
  const file = event.target.files[0];
  if (file) {
    this.sellerService.uploadBanner(file).subscribe({
      next: (res) => {
        if (this.seller) {
          this.seller.bannerImageUrl = res.imageUrl;
        }
      },
      error: () => this.notification.error('Erro ao enviar banner.')
    });
  }
}

  setActiveTab(tab: 'products' | 'about' | 'reviews' | 'moments') {
    this.activeTab = tab;
  }

  goBack() {
    this.location.back();
  }
  startAddMoment() {
  this.isAddingMoment = true;
  this.newMoment = { videoFile: null, thumbFile: null, description: '' };
}

cancelAddMoment() {
  this.isAddingMoment = false;
}

onVideoSelected(event: any) {
  const file = event.target.files[0];
  if (file && file.type.startsWith('video/') && file.size <= 100 * 1024 * 1024) { // máx 100MB
    this.newMoment.videoFile = file;
  } else {
    this.notification.info('Por favor, selecione um vídeo válido (máx 100MB).');
  }
}

onThumbSelected(event: any) {
  const file = event.target.files[0];
  if (file && file.type.startsWith('image/')) {
    this.newMoment.thumbFile = file;
  } else {
    this.notification.info('Por favor, selecione uma imagem válida para a thumbnail.');
  }
}

saveMoment() {
  if (!this.newMoment.videoFile || !this.newMoment.description.trim()) {
    this.notification.warning('Vídeo e descrição são obrigatórios.');
    return;
  }

  this.isUploadingMoment = true;

  // Primeiro: upload do vídeo (com sellerId na URL)
  const sellerId = this.seller.id; // <-- importante: pega o Id do seller

  this.sellerService.uploadMomentVideo(sellerId, this.newMoment.videoFile!).subscribe({
    next: (videoRes: any) => {
      let thumbUrl = '';

      // Se tiver thumbnail, faz upload
      if (this.newMoment.thumbFile) {
        this.sellerService.uploadMomentThumb(sellerId, this.newMoment.thumbFile!).subscribe({
          next: (thumbRes: any) => thumbUrl = thumbRes.imageUrl,
          error: () => console.warn('Falha no upload da thumbnail'),
          complete: () => this.createMoment(videoRes.videoUrl, thumbUrl)
        });
      } else {
        this.createMoment(videoRes.videoUrl, thumbUrl);
      }
    },
    error: (err) => {
      console.error('Erro no upload do vídeo', err);
      this.notification.error('Erro ao enviar vídeo. Verifique o tamanho e formato.');
      this.isUploadingMoment = false;
    }
  });
}
private createMoment(videoUrl: string, thumbUrl: string) {
  const sellerId = this.seller.id;

  const dto = {
    description: this.newMoment.description.trim(),
    videoUrl: videoUrl,
    thumbUrl: thumbUrl || null
  };

  this.sellerService.createMoment(sellerId, dto).subscribe({
    next: (created) => {
      this.moments.unshift(created);
      this.isAddingMoment = false;
      this.isUploadingMoment = false;
      this.notification.success('Momento publicado com sucesso!');
    },
    error: () => {
      this.notification.error('Erro ao salvar o momento.');
      this.isUploadingMoment = false;
    }
  });
}
private finalizeMoment(videoUrl: string, thumbUrl: string) {
  const sellerId = this.seller.id; // ← pega o ID do seller carregado

  const newMomentData = {
    description: this.newMoment.description.trim(),
    videoUrl: videoUrl,
    thumbUrl: thumbUrl || null
  };

  this.sellerService.createMoment(sellerId, newMomentData).subscribe({
    next: (created) => {
      this.moments.unshift({
        id: created.id,
        videoUrl: created.videoUrl,
        thumbUrl: created.thumbUrl || null,
        description: created.description,
        createdAt: created.createdAt
      });
      this.isAddingMoment = false;
      this.isUploadingMoment = false;
      this.notification.success('Momento publicado com sucesso!');
    },
    error: (err) => {
      console.error('Erro ao criar momento', err);
      this.notification.error('Erro ao salvar o momento. Tente novamente.');
      this.isUploadingMoment = false;
    }
  });
}
}
