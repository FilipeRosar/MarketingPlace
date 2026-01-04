import { Component, OnInit, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartOptions } from 'chart.js';
import { AdminService, DashboardStats, PendingSeller, CommissionReportItem } from '../../services/admin/admin.service';
import { NotificationService } from '../../services/notification/notification.service';
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';

interface Notification {
  id: number;
  title: string;
  message: string;
  icon: string; // HTML string do SVG
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

  // --- STATE SIGNALS ---
  stats = signal<DashboardStats>({
    totalGMV: 0,
    totalOrders: 0,
    newUsersLastMonth: 0,
    platformRevenue: 0,
    pendingApprovals: 0
  });

  pendingSellers = signal<PendingSeller[]>([]);
  commissionReport = signal<CommissionReportItem[]>([]);
  customers = signal<any[]>([]);

  // Notificações locais (Toasts)
  notifications = signal<Notification[]>([]);

  // --- UI STATE ---
  activeTab: 'overview' | 'customers' | 'pending' | 'commissions' | 'settings' = 'overview';

  // Buscas
  userSearch = '';
  customerSearch = '';

  // Configurações Globais
  commissionRate = 15;
  serviceFee = 2.99;

  // --- CHART CONFIG ---
  salesChartData: ChartData<'line'> = { labels: [], datasets: [] };
  commissionChartData: ChartData<'bar'> = { labels: [], datasets: [] };

  chartOptions: ChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'bottom' },
      title: { display: false }
    },
    scales: {
      y: { beginAtZero: true, grid: { color: '#f3f4f6' } },
      x: { grid: { display: false } }
    }
  };

  // --- COMPUTED ---
  filteredCustomers = computed(() => {
    const search = this.customerSearch.toLowerCase();
    return this.customers().filter(c =>
      c.name.toLowerCase().includes(search) ||
      c.email.toLowerCase().includes(search)
    );
  });

  ngOnInit() {
    this.loadAllData();
  }

  setActiveTab(tab: any) {
    this.activeTab = tab;
  }

  loadAllData() {
    this.loadStats();
    this.loadPendingSellers();
    this.loadSalesChart();
    this.loadCommissionReport();
    this.loadCustomers();
    // Carregar configs globais se tiver endpoint (simulado aqui)
    this.adminService.getServiceFee().subscribe(res => this.serviceFee = res.fee);
  }

  // --- LOADERS (CONECTADOS AO BACKEND) ---

  loadStats() {
    this.adminService.getDashboardStats().subscribe({
      next: (data) => this.stats.set(data),
      error: () => console.error('Erro ao carregar estatísticas')
    });
  }

  loadPendingSellers() {
    this.adminService.getPendingSellers().subscribe({
      next: (data) => this.pendingSellers.set(data),
      error: () => this.notification.error('Erro ao carregar vendedores pendentes')
    });
  }

  loadCustomers() {
    this.adminService.getCustomers().subscribe({
      next: (data) => this.customers.set(data),
      error: () => this.notification.error('Erro ao carregar clientes')
    });
  }

  loadCommissionReport() {
    this.adminService.getCommissionReport().subscribe({
      next: (data) => {
        this.commissionReport.set(data);
        this.updateCommissionChart(data);
      },
      error: () => this.notification.error('Erro ao carregar relatório financeiro')
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
            backgroundColor: 'rgba(249, 115, 22, 0.1)',
            tension: 0.4,
            fill: true,
            pointBackgroundColor: '#f97316',
            pointRadius: 4
          }]
        };
      }
    });
  }

  updateCommissionChart(report: CommissionReportItem[]) {
    // Top 5 vendedores para o gráfico
    const topSellers = [...report].sort((a, b) => b.totalSales - a.totalSales).slice(0, 5);

    this.commissionChartData = {
      labels: topSellers.map(s => s.sellerName),
      datasets: [
        {
          label: 'Vendas Totais',
          data: topSellers.map(s => s.totalSales),
          backgroundColor: '#fed7aa', // Orange-200
          borderRadius: 4,
          hoverBackgroundColor: '#fdba74'
        },
        {
          label: 'Lucro Plataforma',
          data: topSellers.map(s => s.commissionEarned),
          backgroundColor: '#f97316', // Orange-500
          borderRadius: 4,
          hoverBackgroundColor: '#ea580c'
        }
      ]
    };
  }

  // --- ACTIONS ---

  approveSeller(id: string) {
    this.adminService.approveSeller(id).subscribe({
      next: () => {
        this.addNotification('Sucesso', 'Vendedor aprovado e notificado.', '✅');

        // Optimistic UI: Remove da lista visualmente na hora
        this.pendingSellers.update(list => list.filter(s => s.id !== id));

        // Atualiza contadores em background
        this.loadStats();
      },
      error: () => this.notification.error('Erro ao aprovar vendedor')
    });
  }

  rejectSeller(id: string) {
    if (!confirm('Tem certeza que deseja rejeitar e remover este vendedor?')) return;

    this.adminService.rejectSeller(id).subscribe({
      next: () => {
        this.addNotification('Rejeitado', 'Solicitação de vendedor removida.', '🗑️');

        // Optimistic UI
        this.pendingSellers.update(list => list.filter(s => s.id !== id));

        this.loadStats();
      },
      error: () => this.notification.error('Erro ao rejeitar vendedor')
    });
  }

  // --- SETTINGS & COMMISSIONS ---

  updateCommission() {
    this.adminService.updateCommissionRate(this.commissionRate).subscribe({
      next: () => {
        this.addNotification('Configuração', `Nova taxa global: ${this.commissionRate}%`, '⚙️');
      },
      error: () => this.notification.error('Erro ao atualizar taxa global')
    });
  }

  updateServiceFee() {
    this.adminService.updateServiceFee(this.serviceFee).subscribe({
      next: () => {
        this.addNotification('Configuração', `Nova taxa fixa: R$${this.serviceFee}`, '💰');
      },
      error: () => this.notification.error('Erro ao atualizar taxa fixa')
    });
  }

  // Atualiza taxa de UM vendedor específico na tabela
  updateSellerCommission(seller: CommissionReportItem) {
    if (seller.rate < 0 || seller.rate > 100) {
      this.notification.error('A taxa deve ser entre 0 e 100%');
      return;
    }

    this.adminService.setSellerCommission(seller.sellerId, seller.rate).subscribe({
      next: () => {
        this.addNotification('Atualizado', `Taxa de ${seller.sellerName} definida para ${seller.rate}%`, '🏷️');
      },
      error: () => this.notification.error('Erro ao atualizar taxa individual')
    });
  }

  // Sistema simples de Toast Notifications
  private addNotification(title: string, message: string, icon: string) {
    const id = Date.now();
    this.notifications.update(n => [...n, { id, title, message, icon }]);
    setTimeout(() => this.notifications.update(n => n.filter(x => x.id !== id)), 5000);
  }
}
