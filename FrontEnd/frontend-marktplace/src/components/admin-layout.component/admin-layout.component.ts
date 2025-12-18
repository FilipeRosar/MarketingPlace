// admin-layout.component.ts
import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';
import { NotificationService } from '../../services/notification/notification.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.css'
})
export class AdminLayoutComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  private notificationService = inject(NotificationService);

  // Usuário atual
  currentUser = this.authService.currentUserValue;

  // Contador de sellers pendentes (em tempo real)
  pendingCount = signal(0);

  // Notificações (pra badge no sino)
  notifications = this.notificationService.list;

  // Inicial do nome no avatar
  userInitial = computed(() => {
    const name = this.currentUser?.name || 'A';
    return name.charAt(0).toUpperCase();
  });

  ngOnInit() {
    this.loadPendingCount();
    this.startPendingCountPolling();
  }

  // Carrega quantidade inicial
  private loadPendingCount() {
    // Se tiver um AdminService com getPendingCount()
    // this.adminService.getPendingCount().subscribe(count => this.pendingCount.set(count));
    // Por enquanto, exemplo:
    this.pendingCount.set(3); // substitua pela chamada real
  }

  // Atualiza a cada 30 segundos (ou use SignalR depois)
  private startPendingCountPolling() {
    setInterval(() => {
      this.loadPendingCount();
    }, 30000);
  }

  // Logout
  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  // Fecha menus se necessário (mobile)
  closeMenus() {
    // se tiver menu mobile, fecha aqui
  }
}
