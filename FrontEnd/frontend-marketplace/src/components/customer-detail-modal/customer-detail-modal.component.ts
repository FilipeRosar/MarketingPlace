import { Component, Output, EventEmitter, Input, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../services/admin/admin.service';
import { NotificationService } from '../../services/notification/notification.service';

export interface CustomerDetailModalData {
  id: string;
  name: string;
  email: string;
  phone?: string;
  profileImageUrl?: string;
  createdAt: string;
  totalSpent: number;
}

@Component({
  selector: 'app-customer-detail-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './customer-detail-modal.component.html',
  styleUrl: './customer-detail-modal.component.css'
})
export class CustomerDetailModalComponent implements OnInit {
  @Input({ required: true }) customer!: CustomerDetailModalData;
  @Output() close = new EventEmitter<void>();
  @Output() customerBanned = new EventEmitter<void>();

  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);

  isLoading = signal(false);
  isBanning = signal(false);
  isUnbanning = signal(false);
  customerDetail = signal<any | null>(null);
  detailLoading = signal(true);
  detailError = signal<string | null>(null);

  ngOnInit() {
    this.loadCustomerDetail();
  }

  loadCustomerDetail() {
    this.detailLoading.set(true);
    this.detailError.set(null);

    this.adminService.getCustomerDetail(this.customer.id).subscribe({
      next: (detail) => {
        this.customerDetail.set(detail);
        this.detailLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.detailError.set('Erro ao carregar detalhes do cliente');
        this.detailLoading.set(false);
      }
    });
  }

  onCancel() {
    this.close.emit();
  }

  onBanCustomer() {
    if (!confirm('Tem certeza que deseja banir este cliente? Ele não conseguirá mais fazer login.')) {
      return;
    }

    this.isBanning.set(true);

    this.adminService.banCustomer(this.customer.id).subscribe({
      next: () => {
        this.isBanning.set(false);
        this.notificationService.success('Cliente banido com sucesso!');
        this.customerBanned.emit();
        this.close.emit();
      },
      error: (err) => {
        this.isBanning.set(false);
        console.error(err);
        if (err.status === 400) {
          this.notificationService.error(err.error?.message || 'Cliente já está banido.');
        } else {
          this.notificationService.error('Erro ao banir cliente. Tente novamente.');
        }
      }
    });
  }

  onUnbanCustomer() {
    if (!confirm('Tem certeza que deseja desbannir este cliente? Ele voltará a poder fazer login.')) {
      return;
    }

    this.isUnbanning.set(true);

    this.adminService.unbanCustomer(this.customer.id).subscribe({
      next: () => {
        this.isUnbanning.set(false);
        this.notificationService.success('Cliente desbannido com sucesso!');
        this.customerBanned.emit();
        this.close.emit();
      },
      error: (err) => {
        this.isUnbanning.set(false);
        console.error(err);
        if (err.status === 400) {
          this.notificationService.error(err.error?.message || 'Cliente não está banido.');
        } else {
          this.notificationService.error('Erro ao desbannir cliente. Tente novamente.');
        }
      }
    });
  }

  formatDate(dateString: string): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('pt-BR', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(value);
  }
}
