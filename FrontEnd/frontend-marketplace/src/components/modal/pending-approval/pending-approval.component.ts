import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-pending-approval',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen bg-surface flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div class="sm:mx-auto sm:w-full sm:max-w-md">
        <div class="bg-white py-8 px-4 shadow-soft rounded-2xl sm:px-10 border border-stone-100 text-center">

          <div class="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-yellow-100 mb-6">
            <svg class="h-8 w-8 text-yellow-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>

          <h2 class="text-2xl font-bold text-gray-900 mb-2">Conta em Análise</h2>

          <p class="text-gray-500 mb-6">
            Obrigado por registrar sua loja! Nossa equipe está analisando seus dados.
            Assim que sua conta for aprovada, você receberá um e-mail e poderá começar a vender.
          </p>

          <div class="space-y-3">
            <a routerLink="/" class="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-primary hover:bg-primary-dark transition-colors">
              Voltar para a Home
            </a>
            <button class="w-full flex justify-center py-2 px-4 border border-stone-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 transition-colors">
              Falar com Suporte
            </button>
          </div>

        </div>
      </div>
    </div>
  `
})
export class PendingApprovalComponent {}
