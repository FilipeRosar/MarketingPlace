import { Component, inject } from '@angular/core';
import { AuthService } from '../../../services/auth/auth.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-pending-approval',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-amber-50 via-orange-50 to-red-50 flex items-center justify-center px-4">
      <div class="bg-white/95 backdrop-blur-sm rounded-3xl shadow-2xl p-16 max-w-2xl text-center border-8 border-orange-300">
        <div class="text-9xl mb-8">Artist Palette</div>
        <h1 class="text-6xl font-black text-orange-900 mb-8">Aguardando Aprovação</h1>
        <p class="text-2xl text-orange-800 mb-6 font-medium">
          Olá, {{ userName }}! Sua loja está em análise.
        </p>
        <p class="text-xl text-gray-700 mb-10 leading-relaxed">
          Nosso time está revisando seu cadastro com carinho.<br>
          Você receberá um e-mail assim que for aprovado(a).
        </p>
        <p class="text-lg text-orange-600 font-bold mb-12">
          Enquanto isso, prepare suas artes! Em breve você estará vendendo na Trama!
        </p>
        <button routerLink="/" class="bg-gradient-to-r from-orange-600 to-red-600 hover:from-orange-700 hover:to-red-700 text-white font-black text-2xl py-6 px-16 rounded-full shadow-2xl hover:shadow-3xl transition-all transform hover:-translate-y-2">
          Voltar para Home
        </button>
      </div>
    </div>
  `
})
export class PendingApprovalComponent {
  userName = inject(AuthService).currentUserValue?.name || 'Artesão';
}
