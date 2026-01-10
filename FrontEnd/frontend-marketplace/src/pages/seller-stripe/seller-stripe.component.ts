import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SellerService } from '../../services/seller/seller.service';

interface StripeStatus {
  isConnected: boolean;
  accountId?: string;
  chargesEnabled?: boolean;
  detailsSubmitted?: boolean;
}

@Component({
  selector: 'app-seller-stripe',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './seller-stripe.component.html',
  styleUrl: './seller-stripe.component.css'
})
export class SellerStripeComponent implements OnInit {
  private sellerService = inject(SellerService);
  private route = inject(ActivatedRoute);

  stripeStatus: StripeStatus | null = null;
  isLoading = false;
  isWorking = false;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.loadStatus();

    this.route.queryParamMap.subscribe(params => {
      if (params.get('stripe')) {
        this.loadStatus();
      }
    });
  }

  loadStatus() {
    this.isLoading = true;
    this.errorMessage = null;
    this.sellerService.getStripeStatus().subscribe({
      next: (status) => {
        this.stripeStatus = status;
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.message || 'Erro ao carregar status do Stripe.';
      }
    });
  }

  connectStripe() {
    if (this.isWorking) return;
    this.isWorking = true;
    this.sellerService.createStripeConnectLink().subscribe({
      next: (res) => {
        window.location.href = res.url;
      },
      error: (err) => {
        this.isWorking = false;
        this.errorMessage = err?.error?.message || 'Erro ao iniciar conexao com Stripe.';
      }
    });
  }

  manageStripe() {
    if (this.isWorking) return;
    this.isWorking = true;
    this.sellerService.createStripeDashboardLink().subscribe({
      next: (res) => {
        window.location.href = res.url;
      },
      error: (err) => {
        this.isWorking = false;
        this.errorMessage = err?.error?.message || 'Erro ao abrir portal do Stripe.';
      }
    });
  }
}
