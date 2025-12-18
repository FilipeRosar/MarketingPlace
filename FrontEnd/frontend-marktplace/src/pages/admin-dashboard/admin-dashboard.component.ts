// admin-dashboard.component.ts
import { Component, OnInit, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartOptions } from 'chart.js';
import { AdminService, DashboardStats, PendingSeller } from '../../services/admin/admin.service';
import { NotificationService } from '../../services/notification/notification.service';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';

interface User {
  id: string;
  name: string;
  email: string;
  role: 'Admin' | 'Seller' | 'Customer';
  phone?: string;
}

interface CommissionReport {
  sellerId: string;
  sellerName: string;
  totalSales: number;
  commissionEarned: number;
  rate: number;
}

interface Notification {
  id: number;
  title: string;
  message: string;
  icon: string;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, BaseChartDirective, CurrencyBrPipe],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit {
  private adminService = inject(AdminService);
  private notification = inject(NotificationService);
  // Dados principais
  stats = signal<DashboardStats>({
  totalGMV: 0,
  totalOrders: 0,
  newUsersLastMonth: 0,
  platformRevenue: 0,
  pendingApprovals: 0
  });

  pendingSellers = signal<PendingSeller[]>([]);
  allUsers = signal<User[]>([]);
  commissionReport = signal<CommissionReport[]>([]);
  activeTab: 'overview' | 'customers' | 'pending' | 'commissions' | 'settings' = 'overview';
  // Busca
  userSearch = '';
  customers = signal<any[]>([]);
  customerSearch = '';
  // Configurações
  commissionRate = 15;
  serviceFee = 2.99;

  // Notificações
  notifications = signal<Notification[]>([]);

  // Filtro de usuários
  filteredUsers = computed(() => {
    const search = this.userSearch.toLowerCase();
    return this.allUsers().filter(user =>
      user.name.toLowerCase().includes(search) ||
      user.email.toLowerCase().includes(search)
    );
  });
  setActiveTab(tab: 'overview' | 'customers' | 'pending' | 'commissions' | 'settings') {
    this.activeTab = tab;
  }
  filteredCustomers = computed(() => {
    const search = this.customerSearch.toLowerCase();
    return this.customers().filter(c =>
      c.name.toLowerCase().includes(search) ||
      c.email.toLowerCase().includes(search)
    );
  });
  loadCustomers() {
  this.adminService.getCustomers().subscribe({
    next: (data) => this.customers.set(data),
    error: () => this.notification.error('Erro ao carregar clientes')
  });
}
  // Gráficos
  salesChartData: ChartData<'line'> = {
    labels: ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun'],
    datasets: [
      {
        label: 'Vendas (R$)',
        data: [12000, 19000, 15000, 25000, 22000, 30000],
        borderColor: '#f97316',
        backgroundColor: 'rgba(249, 115, 22, 0.2)',
        tension: 0.4,
        fill: true
      }
    ]
  };

  commissionChartData: ChartData<'bar'> = {
    labels: ['Artesão A', 'Artesão B', 'Artesão C', 'Artesão D'],
    datasets: [
      {
        label: 'Receita Bruta',
        data: [15000, 12000, 18000, 9000],
        backgroundColor: '#f97316'
      },
      {
        label: 'Comissão Plataforma',
        data: [2250, 1800, 2700, 1350],
        backgroundColor: '#ef4444'
      }
    ]
  };

  chartOptions: ChartOptions = {
    responsive: true,
    plugins: {
      legend: { position: 'bottom' as const },
      title: { display: false }
    },
    scales: {
      y: { beginAtZero: true }
    }
  };

  ngOnInit() {
  this.loadStats();
  this.loadPendingSellers();
  this.loadSalesChart();
  this.loadCommissionReport();
  this.loadCustomers();
}

  loadAllData() {
    this.loadStats();
    this.loadPendingSellers();
    this.loadUsers();
    this.loadCommissionReport();
  }

  loadStats() {
    this.adminService.getDashboardStats().subscribe({
      next: (data) => this.stats.set(data),
      error: () => this.notification.error('Erro ao carregar estatísticas')
    });
  }
  loadSalesChart() {
  this.adminService.getSalesByMonth().subscribe({
    next: (data) => {
      this.salesChartData = {
        labels: data.map(d => d.month),
        datasets: [{
          label: 'Vendas (R$)',
          data: data.map(d => d.total),
          borderColor: '#f97316',
          backgroundColor: 'rgba(249, 115, 22, 0.2)',
          tension: 0.4,
          fill: true
        }]
      };
    },
    error: () => this.notification.error('Erro ao carregar dados de vendas')
  });
}

  loadPendingSellers() {
    this.adminService.getPendingSellers().subscribe({
      next: (sellers) => this.pendingSellers.set(sellers),
      error: () => this.notification.error('Erro ao carregar vendedores pendentes')
    });
  }

  loadUsers() {
    // Exemplo — substitua por chamada real ao backend
    this.allUsers.set([
      { id: '1', name: 'João Artesão', email: 'joao@trama.com', role: 'Seller', phone: '(11) 99999-9999' },
      { id: '2', name: 'Maria Cliente', email: 'maria@email.com', role: 'Customer', phone: '(11) 88888-8888' },
      { id: '3', name: 'Admin Trama', email: 'admin@trama.com', role: 'Admin' }
    ]);
  }

  loadCommissionReport() {
    // Exemplo — substitua por chamada real
    this.commissionReport.set([
      { sellerId: '1', sellerName: 'João Artesão', totalSales: 15000, commissionEarned: 2250, rate: 15 },
      { sellerId: '2', sellerName: 'Ana Cerâmica', totalSales: 12000, commissionEarned: 1800, rate: 15 },
      { sellerId: '3', sellerName: 'Pedro Madeira', totalSales: 18000, commissionEarned: 2700, rate: 15 }
    ]);
  }

  approveSeller(id: string) {
    this.adminService.approveSeller(id).subscribe({
      next: () => {
        this.notification.success('Vendedor aprovado com sucesso!');
        this.addNotification('Novo Artesão Aprovado!', 'Bem-vindo à Trama!', '🎨');
        this.loadAllData();
      },
      error: () => this.notification.error('Erro ao aprovar vendedor')
    });
  }

  rejectSeller(id: string) {
    this.adminService.rejectSeller(id).subscribe({
      next: () => {
        this.notification.warning('Vendedor rejeitado');
        this.addNotification('Vendedor Rejeitado', 'Solicitação negada.', '❌');
        this.loadAllData();
      },
      error: () => this.notification.error('Erro ao rejeitar vendedor')
    });
  }

  updateCommission() {
    this.adminService.updateCommissionRate(this.commissionRate).subscribe({
      next: () => {
        this.notification.success('Taxa de comissão atualizada!');
        this.addNotification('Configuração Atualizada', `Comissão: ${this.commissionRate}%`, '⚙️');
      },
      error: () => this.notification.error('Erro ao atualizar comissão')
    });
  }

  updateServiceFee() {
    this.adminService.updateServiceFee(this.serviceFee).subscribe({
      next: () => {
        this.notification.success('Taxa de serviço atualizada!');
        this.addNotification('Configuração Atualizada', `Taxa de serviço: R$${this.serviceFee.toFixed(2)}`, '💰');
      },
      error: () => this.notification.error('Erro ao atualizar taxa')
    });
  }

  deleteUser(id: string) {
    if (confirm('Tem certeza que deseja excluir este usuário?')) {
      // Chame o service quando tiver
      this.notification.warning('Usuário excluído');
      this.allUsers.update(users => users.filter(u => u.id !== id));
    }
  }

  private addNotification(title: string, message: string, icon: string) {
    const id = Date.now();
    this.notifications.update(notifs => [...notifs, { id, title, message, icon }]);

    setTimeout(() => {
      this.notifications.update(notifs => notifs.filter(n => n.id !== id));
    }, 5000);
  }
}
