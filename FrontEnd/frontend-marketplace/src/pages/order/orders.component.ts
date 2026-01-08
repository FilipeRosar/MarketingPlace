import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrderService } from '../../services/order/order.service';
import { Order } from '../../models/order/order.model'; 
import { CurrencyBrPipe } from '../../shared/pipes/currency-br-pipe';
import { LoadingSpinnerComponent } from '../../components/loading-spinner.component/loading-spinner.component';
import { ProductService } from '../../services/product/product.service';
import { CartService } from '../../services/cart/cart.service';
import { NotificationService } from '../../services/notification/notification.service';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, RouterLink, CurrencyBrPipe, DatePipe, LoadingSpinnerComponent],
  templateUrl: './orders.html',
  styleUrl: './orders.css'
})
export class OrdersComponent implements OnInit {
  private orderService = inject(OrderService);
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private notificationService = inject(NotificationService);

  orders: Order[] = [];
  isLoading = true;
  errorMessage = '';
  addingProductIds = new Set<string>();

  // Mapeamento de Status para Texto e Cor
  // Ajuste conforme o Enum do seu backend (0=Pending, 1=Confirmed, etc)
  statusMap: any = {
    0: { label: 'Pendente', classes: 'bg-yellow-100 text-yellow-800' },
    1: { label: 'Confirmado', classes: 'bg-green-100 text-green-800' },
    2: { label: 'Processando', classes: 'bg-orange-100 text-orange-800' },
    3: { label: 'Enviado', classes: 'bg-blue-100 text-blue-800' },
    4: { label: 'Entregue', classes: 'bg-purple-100 text-purple-800' },
    5: { label: 'Cancelado', classes: 'bg-red-100 text-red-800' },
    6: { label: 'Reembolsado', classes: 'bg-gray-200 text-gray-700' }
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

  cancelOrder(orderId: string) {
    if (!confirm('Tem certeza que deseja cancelar este pedido?')) {
      return;
    }

    this.orderService.cancelOrder(orderId).subscribe({
      next: () => this.loadOrders(),
      error: (err) => {
        console.error('Erro ao cancelar pedido', err);
        this.errorMessage = 'Não foi possível cancelar o pedido. Tente novamente mais tarde.';
      }
    });
  }

  isCancelable(order: Order): boolean {
    return order.status === 0 || order.status === 1;
  }

  buyAgain(item: { productId: string; productName: string }) {
    if (this.addingProductIds.has(item.productId)) return;

    this.addingProductIds.add(item.productId);
    this.productService.getProductById(item.productId).subscribe({
      next: (product) => {
        this.cartService.addToCart(product);
        this.addingProductIds.delete(item.productId);
      },
      error: () => {
        this.addingProductIds.delete(item.productId);
        this.notificationService.error(
          `Não foi possível adicionar "${item.productName}" ao carrinho.`
        );
      }
    });
  }

  getStatusInfo(status: any) {
    let key = status;
    if (typeof status === 'string') {
        const statusLower = status.toLowerCase();
        if (statusLower === 'pending') key = 0;
        else if (statusLower === 'paid' || statusLower === 'confirmed') key = 1;
        else if (statusLower === 'processing') key = 2;
        else if (statusLower === 'shipped' || statusLower === 'sent') key = 3;
        else if (statusLower === 'delivered') key = 4;
        else if (statusLower === 'canceled') key = 5;
        else if (statusLower === 'refunded') key = 6;
    }
    return this.statusMap[key] || { label: 'Desconhecido', classes: 'bg-gray-100 text-gray-800' };
  }
}
