import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen bg-surface py-12 px-4 sm:px-6 lg:px-8 flex justify-center">
      <div class="max-w-md w-full text-center">
        <h1 class="text-3xl font-bold text-gray-900 mb-4">Meus Pedidos</h1>
        <p class="text-gray-600">Aqui você verá o histórico de suas compras.</p>
      </div>
    </div>
  `
})
export class OrdersComponent {}
