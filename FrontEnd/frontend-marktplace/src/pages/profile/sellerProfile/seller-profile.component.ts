import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SellerService } from '../../../services/seller/seller.service';
import { ProductService } from '../../../services/product/product.service';
import { AuthService } from '../../../services/auth/auth.service';
import { Seller, Product } from '../../../models/product/product.model';
import { ProductCardComponent } from '../../../components/product-card/product-card.component';
import { LoadingSpinnerComponent } from '../../../components/loading-spinner.component/loading-spinner.component';

@Component({
  selector: 'app-seller-profile',
  standalone: true,
  imports: [CommonModule, RouterLink, ProductCardComponent, LoadingSpinnerComponent],
  templateUrl: './seller-profile.html',
  styleUrl: './seller-profile.css'
})
export class SellerProfileComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private sellerService = inject(SellerService);
  private productService = inject(ProductService);
  private authService = inject(AuthService); // Injetar Auth
  private location = inject(Location);

  seller: any | null = null;
  products: Product[] = [];
  isLoading = true;

  // Verifica se é o dono da página
  isOwner = false;

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
    this.productService.getAllProducts(1, 100).subscribe({
      next: (response: any) => {
        const all = Array.isArray(response) ? response : (response.data || response.items || []);
        this.products = all.filter((p: any) => p.sellerId === sellerId);
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  goBack() {
    this.location.back();
  }
}
