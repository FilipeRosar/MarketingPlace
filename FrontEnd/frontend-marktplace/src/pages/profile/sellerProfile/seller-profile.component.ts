import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, Location } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SellerService } from '../../../services/seller/seller.service';
import { ProductService } from '../../../services/product/product.service';
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
  private location = inject(Location);

  seller: any | null = null;
  products: Product[] = [];
  isLoading = true;

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const sellerId = params.get('id');
      if (sellerId) {
        this.loadSellerData(sellerId);
      }
    });
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
    // Precisamos atualizar o ProductService para aceitar o filtro,
    // mas como ajustamos o backend para aceitar ?sellerId=XYZ,
    // vamos chamar passando params manuais se o serviço não tiver o argumento ainda,
    // ou usar o método getAllProducts e filtrar no client (menos performático mas funciona agora)

    // Opção ideal: Chamar com query param. Vou assumir que você atualizará o ProductService
    // ou vamos fazer uma chamada direta aqui para agilizar:

    this.productService.getAllProducts(1, 100).subscribe({
      next: (response: any) => {
        const all = Array.isArray(response) ? response : (response.data || response.items || []);
        // Filtra no cliente por garantia
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
