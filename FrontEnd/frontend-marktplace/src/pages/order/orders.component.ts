import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrderService } from '../../services/order/order.service';
import { Order } from '../../models/order/order.model'; // Importar o modelo correto
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyBrPipe, DatePipe, LoadingSpinnerComponent],
  templateUrl: './orders.html',
  styleUrl: './orders.css' 
})
export class OrdersComponent implements OnInit {
  private orderService = inject(OrderService);

  orders: Order[] = [];
  isLoading = true;
  errorMessage = '';

  // Mapeamento de Status para Texto e Cor
  // Ajuste conforme o Enum do seu backend (0=Pending, 1=Paid, etc)
  statusMap: any = {
    0: { label: 'Pendente', classes: 'bg-yellow-100 text-yellow-800' },
    1: { label: 'Pago', classes: 'bg-green-100 text-green-800' },
    2: { label: 'Enviado', classes: 'bg-blue-100 text-blue-800' },
    3: { label: 'Entregue', classes: 'bg-purple-100 text-purple-800' },
    4: { label: 'Cancelado', classes: 'bg-red-100 text-red-800' }
  };

  ngOnInit() {
    this.loadOrders();
  }

  loadOrders() {
    this.isLoading = true;
    this.orderService.getMyOrders().subscribe({
      next: (data) => {
        console.log('Pedidos carregados:', data);
        this.orders = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erro ao carregar pedidos', err);
        this.errorMessage = 'Não foi possível carregar seu histórico de compras. Tente novamente mais tarde.';
        this.isLoading = false;
      }
    });
  }

  getStatusInfo(status: any) {
    let key = status;
    if (typeof status === 'string') {
        const statusLower = status.toLowerCase();
        if (statusLower === 'pending') key = 0;
        else if (statusLower === 'paid') key = 1;
        else if (statusLower === 'shipped' || statusLower === 'sent') key = 2;
        else if (statusLower === 'delivered') key = 3;
        else if (statusLower === 'canceled') key = 4;
    }
    return this.statusMap[key] || { label: 'Desconhecido', classes: 'bg-gray-100 text-gray-800' };
  }
}
