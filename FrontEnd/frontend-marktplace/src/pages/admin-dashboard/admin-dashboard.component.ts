import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { AdminService } from '../../services/admin/admin.service';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, CurrencyBrPipe, DatePipe],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit {
  private adminService = inject(AdminService);

  activeTab: 'stats' | 'sellers' = 'stats';
  pendingSellers: any[] = [];
  stats: any = null;
  isLoading = false;

  ngOnInit() {
    this.loadStats();
  }

  setTab(tab: 'stats' | 'sellers') {
    this.activeTab = tab;
    if (tab === 'sellers') this.loadSellers();
  }

  loadStats() {
    this.adminService.getStats().subscribe(data => this.stats = data);
  }

  loadSellers() {
    this.isLoading = true;
    this.adminService.getPendingSellers().subscribe({
      next: (data) => {
        this.pendingSellers = data;
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  onApprove(seller: any) {
    if(confirm(`Aprovar a loja "${seller.name}"?`)) {
      this.adminService.approveSeller(seller.id).subscribe(() => {
        alert('Vendedor aprovado!');
        this.loadSellers();
      });
    }
  }

  onReject(seller: any) {
    if(confirm(`Rejeitar/Remover "${seller.name}"?`)) {
      this.adminService.rejectSeller(seller.id).subscribe(() => {
        this.loadSellers();
      });
    }
  }
}
